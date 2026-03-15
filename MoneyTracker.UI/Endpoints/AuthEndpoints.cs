using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.UI.Endpoints
{
    public static class AuthEndpoints
    {
        public static WebApplication MapAuthEndpoints(this WebApplication app)
        {
            var authGroup = app.MapGroup("/auth");

            authGroup.MapPost("/login", async (
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                HttpRequest request,
                SignInManager<ApplicationUser> signInManager,
                UserManager<ApplicationUser> userManager,
                IEmailSenderService emailSenderService,
                IVerificationCodeService verificationCodeService,
                IAuthAuditService authAuditService,
                IErrorLogService errorLogService,
                IValidator<LoginRequestDto> validator) =>
            {
                var dto = new LoginRequestDto
                {
                    Email = email,
                    Password = password,
                    RememberMe = rememberMe
                };

                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
                    await LogHandledAsync(errorLogService, $"Login validation failed. {error}");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, false, "ValidationFailed", null, null, null);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, error, null));
                }

                var normalizedEmail = email.Trim().ToLowerInvariant();
                var maskedEmail = MaskEmail(normalizedEmail);
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null)
                {
                    await LogHandledAsync(errorLogService, $"Login failed because user {maskedEmail} was not found.");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, false, "UserNotFound", maskedEmail, null, null);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Invalid email or password.", null));
                }

                var preCheck = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
                if (preCheck.IsLockedOut)
                {
                    await LogHandledAsync(errorLogService, $"Login failed because user {maskedEmail} is locked out.");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, false, "LockedOut", maskedEmail, user.Id, user.TenantId);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Your account is temporarily locked. Try again later.", null));
                }

                if (preCheck.IsNotAllowed)
                {
                    await LogHandledAsync(errorLogService, $"Login blocked because user {maskedEmail} has not confirmed email.");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, false, "EmailNotConfirmed", maskedEmail, user.Id, user.TenantId);
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&error={Uri.EscapeDataString("Email confirmation is required before sign in.")}");
                }

                if (!preCheck.Succeeded)
                {
                    await LogHandledAsync(errorLogService, $"Login failed because password validation did not succeed for {maskedEmail}.");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, false, "InvalidPassword", maskedEmail, user.Id, user.TenantId);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Invalid email or password.", null));
                }

                var result = await signInManager.PasswordSignInAsync(
                    user,
                    password,
                    rememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, true, null, maskedEmail, user.Id, user.TenantId);
                    return Results.LocalRedirect(SafeReturnUrl(returnUrl));
                }

                if (!result.RequiresTwoFactor)
                {
                    await LogHandledAsync(errorLogService, $"Login failed because sign-in did not complete for {maskedEmail}.");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, false, "SignInIncomplete", maskedEmail, user.Id, user.TenantId);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Invalid email or password.", null));
                }

                var codeResult = await verificationCodeService.IssueCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.LoginOtp,
                    expiryMinutes: 10,
                    resendCooldownSeconds: 60,
                    maxAttempts: 3);

                if (!codeResult.Success || codeResult.Data is null)
                {
                    await LogHandledAsync(errorLogService, $"OTP issue failed during login for {maskedEmail}. {codeResult.Message}");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, false, "OtpIssueFailed", maskedEmail, user.Id, user.TenantId);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, codeResult.Message, null));
                }

                var sendResult = await emailSenderService.SendAsync(
                    user.Email!,
                    "Your MoneyTracker login code",
                    $"<p>Your OTP code is:</p><h2>{codeResult.Data.Code}</h2><p>The code expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

                if (!sendResult.Success)
                {
                    await LogHandledAsync(errorLogService, $"OTP email send failed during login for {maskedEmail}. {sendResult.Message}");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, false, "OtpEmailSendFailed", maskedEmail, user.Id, user.TenantId);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, sendResult.Message, null));
                }

                return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, null, "A verification code was sent to your email.", codeResult.Data.ExpiresAtUtc));
            }).AllowAnonymous();

            authGroup.MapPost("/login-otp", async (
                [FromForm] string code,
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                [FromForm] string? expiresAtUtc,
                HttpRequest request,
                SignInManager<ApplicationUser> signInManager,
                UserManager<ApplicationUser> userManager,
                IVerificationCodeService verificationCodeService,
                IAuthAuditService authAuditService,
                IErrorLogService errorLogService,
                IValidator<LoginOtpRequestDto> validator) =>
            {
                var dto = new LoginOtpRequestDto { Code = code };
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
                    await LogHandledAsync(errorLogService, $"OTP validation failed. {error}");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpVerify, false, "ValidationFailed", null, null, null);
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, error, null, ParseUtc(expiresAtUtc)));
                }

                var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user is null)
                {
                    await LogHandledAsync(errorLogService, "OTP verification failed because the auth session expired.");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpVerify, false, "SessionExpired", null, null, null);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Session expired. Please sign in again.", null));
                }

                if (await userManager.IsLockedOutAsync(user))
                {
                    await LogHandledAsync(errorLogService, $"OTP verification failed because user {MaskEmail(user.Email)} is locked out.");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpVerify, false, "LockedOut", MaskEmail(user.Email), user.Id, user.TenantId);
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, "Your account is temporarily locked. Try again later.", null, ParseUtc(expiresAtUtc)));
                }

                var cleanedCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);
                var verifyResult = await verificationCodeService.VerifyCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.LoginOtp,
                    cleanedCode);

                if (!verifyResult.Success)
                {
                    await userManager.AccessFailedAsync(user);
                    if (await userManager.IsLockedOutAsync(user))
                    {
                        await LogHandledAsync(errorLogService, $"OTP verification locked out user {MaskEmail(user.Email)}.");
                        await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpVerify, false, "LockedOutAfterOtpFailure", MaskEmail(user.Email), user.Id, user.TenantId);
                        return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, "Your account is temporarily locked. Try again later.", null, ParseUtc(expiresAtUtc)));
                    }

                    await LogHandledAsync(errorLogService, $"OTP verification failed for user {MaskEmail(user.Email)}. Invalid or expired code.");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpVerify, false, "InvalidOrExpiredCode", MaskEmail(user.Email), user.Id, user.TenantId);
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, "Invalid or expired verification code.", null, ParseUtc(expiresAtUtc)));
                }

                await userManager.ResetAccessFailedCountAsync(user);
                await signInManager.SignInAsync(user, rememberMe);
                await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpVerify, true, null, MaskEmail(user.Email), user.Id, user.TenantId);
                return Results.LocalRedirect(SafeReturnUrl(returnUrl));
            }).AllowAnonymous();

            authGroup.MapPost("/login-otp/resend", async (
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                HttpRequest request,
                SignInManager<ApplicationUser> signInManager,
                IEmailSenderService emailSenderService,
                IVerificationCodeService verificationCodeService,
                IAuthAuditService authAuditService,
                IErrorLogService errorLogService) =>
            {
                var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user is null)
                {
                    await LogHandledAsync(errorLogService, "OTP resend failed because the auth session expired.");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpResend, false, "SessionExpired", null, null, null);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Session expired. Please sign in again.", null));
                }

                var codeResult = await verificationCodeService.IssueCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.LoginOtp,
                    expiryMinutes: 10,
                    resendCooldownSeconds: 60,
                    maxAttempts: 3);

                if (!codeResult.Success || codeResult.Data is null)
                {
                    await LogHandledAsync(errorLogService, $"OTP resend issue failed for user {MaskEmail(user.Email)}. {codeResult.Message}");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpResend, false, "OtpIssueFailed", MaskEmail(user.Email), user.Id, user.TenantId);
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, codeResult.Message, null, null));
                }

                var sendResult = await emailSenderService.SendAsync(
                    user.Email!,
                    "Your MoneyTracker login code",
                    $"<p>Your OTP code is:</p><h2>{codeResult.Data.Code}</h2><p>The code expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

                if (!sendResult.Success)
                {
                    await LogHandledAsync(errorLogService, $"OTP resend email failed for user {MaskEmail(user.Email)}. {sendResult.Message}");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpResend, false, "OtpEmailSendFailed", MaskEmail(user.Email), user.Id, user.TenantId);
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, sendResult.Message, null, null));
                }

                await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginOtpResend, true, null, MaskEmail(user.Email), user.Id, user.TenantId);
                return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, null, "A verification code was sent to your email.", codeResult.Data.ExpiresAtUtc));
            }).AllowAnonymous();

            authGroup.MapPost("/resend-confirmation", async (
                [FromForm] string email,
                HttpRequest request,
                UserManager<ApplicationUser> userManager,
                IEmailSenderService emailSenderService,
                IErrorLogService errorLogService,
                IOptions<ApplicationSettings> applicationOptions,
                IHostEnvironment hostEnvironment) =>
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var maskedEmail = MaskEmail(normalizedEmail);
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null)
                {
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&info={Uri.EscapeDataString("If the email exists, a confirmation link was sent.")}");
                }

                if (await userManager.IsEmailConfirmedAsync(user))
                {
                    await LogHandledAsync(errorLogService, $"Confirmation resend skipped because {maskedEmail} is already confirmed.");
                    return Results.LocalRedirect(BuildLoginUrl(null, null, "Email already confirmed. You can sign in."));
                }

                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
                var confirmationUrl = BuildAbsoluteUrl(
                    request,
                    applicationOptions.Value,
                    hostEnvironment,
                    $"/confirm-email?userId={Uri.EscapeDataString(user.Id.ToString())}&code={Uri.EscapeDataString(encodedToken)}");
                if (string.IsNullOrWhiteSpace(confirmationUrl))
                {
                    await LogHandledAsync(errorLogService, "Confirmation email could not be prepared because PublicBaseUrl is missing.");
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&error={Uri.EscapeDataString("PublicBaseUrl must be configured before sending confirmation emails.")}");
                }
                var sendResult = await emailSenderService.SendAsync(
                    user.Email!,
                    "Confirm your MoneyTracker email",
                    $"<p>Confirm your account:</p><p><a href=\"{confirmationUrl}\">{confirmationUrl}</a></p>");

                if (!sendResult.Success)
                {
                    await LogHandledAsync(errorLogService, $"Confirmation email failed for {maskedEmail}. {sendResult.Message}");
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&error={Uri.EscapeDataString(sendResult.Message)}");
                }

                return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&info={Uri.EscapeDataString("Confirmation link sent. Check your email.")}");
            }).AllowAnonymous();

            authGroup.MapPost("/register", async (
                [FromForm] string displayName,
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] string confirmPassword,
                UserManager<ApplicationUser> userManager,
                MoneyTrackerDbContext dbContext,
                ITenantBootstrapService tenantBootstrapService,
                IErrorLogService errorLogService,
                IOptions<ApplicationSettings> applicationOptions,
                IValidator<RegisterRequestDto> validator) =>
            {
                if (!applicationOptions.Value.EnablePublicRegistration)
                {
                    await LogHandledAsync(errorLogService, "Public registration attempt rejected because it is disabled by configuration.");
                    return Results.LocalRedirect($"/register?error={Uri.EscapeDataString("Public registration is currently disabled.")}");
                }

                var dto = new RegisterRequestDto
                {
                    DisplayName = displayName,
                    Email = email,
                    Password = password,
                    ConfirmPassword = confirmPassword
                };

                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
                    await LogHandledAsync(errorLogService, $"Public registration validation failed. {error}");
                    return Results.LocalRedirect($"/register?error={Uri.EscapeDataString(error)}");
                }

                var normalizedEmail = email.Trim().ToLowerInvariant();
                var maskedEmail = MaskEmail(normalizedEmail);
                var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
                if (existingUser is not null)
                {
                    await LogHandledAsync(errorLogService, $"Public registration skipped because {maskedEmail} already has an account.");
                    return Results.LocalRedirect($"/login?info={Uri.EscapeDataString("This email already has an account. Please sign in.")}");
                }

                var tenantId = Guid.NewGuid();
                var user = new ApplicationUser
                {
                    UserName = normalizedEmail,
                    Email = normalizedEmail,
                    EmailConfirmed = true,
                    TenantId = tenantId,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                    TwoFactorEnabled = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var error = string.Join(" ", createResult.Errors.Select(x => x.Description));
                    await LogHandledAsync(errorLogService, $"Public registration user creation failed for {maskedEmail}. {error}");
                    return Results.LocalRedirect($"/register?error={Uri.EscapeDataString(error)}");
                }

                try
                {
                    dbContext.Tenants.Add(new Tenant
                    {
                        TenantId = tenantId,
                        Name = BuildTenantName(displayName, normalizedEmail),
                        OwnerUserId = user.Id
                    });

                    await dbContext.SaveChangesAsync();
                    await tenantBootstrapService.SeedDefaultsAsync(tenantId);

                    return Results.LocalRedirect("/login?info=Account%20created%20successfully.%20You%20can%20sign%20in.");
                }
                catch (Exception ex)
                {
                    await errorLogService.LogExceptionAsync(ex, $"Public registration failed for {maskedEmail}.");
                    await userManager.DeleteAsync(user);
                    return Results.LocalRedirect($"/register?error={Uri.EscapeDataString("Registration failed. Please try again.")}");
                }
            }).AllowAnonymous();

            authGroup.MapPost("/invite-register", async (
                [FromForm] string token,
                [FromForm] string displayName,
                [FromForm] string password,
                [FromForm] string confirmPassword,
                UserManager<ApplicationUser> userManager,
                MoneyTrackerDbContext dbContext,
                ITenantBootstrapService tenantBootstrapService,
                IUserInvitationService userInvitationService,
                IErrorLogService errorLogService,
                IValidator<AcceptInvitationRequestDto> validator) =>
            {
                var dto = new AcceptInvitationRequestDto
                {
                    Token = token,
                    DisplayName = displayName,
                    Password = password,
                    ConfirmPassword = confirmPassword
                };

                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
                    await LogHandledAsync(errorLogService, $"Invite registration validation failed. {error}");
                    return Results.LocalRedirect($"/invite/register?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(error)}");
                }

                var invitationResult = await userInvitationService.GetValidInvitationAsync(token);
                if (!invitationResult.Success || invitationResult.Data is null)
                {
                    await LogHandledAsync(errorLogService, $"Invite registration failed because invitation token was invalid. {invitationResult.Message}");
                    return Results.LocalRedirect($"/invite/register?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(invitationResult.Message)}");
                }

                var normalizedEmail = invitationResult.Data.Email.Trim().ToLowerInvariant();
                var maskedEmail = MaskEmail(normalizedEmail);
                var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
                if (existingUser is not null)
                {
                    await LogHandledAsync(errorLogService, $"Invite registration skipped because {maskedEmail} already has an account.");
                    return Results.LocalRedirect($"/login?info={Uri.EscapeDataString("This invitation email already has an account. Please sign in.")}");
                }

                var tenantId = Guid.NewGuid();
                var user = new ApplicationUser
                {
                    UserName = normalizedEmail,
                    Email = normalizedEmail,
                    EmailConfirmed = true,
                    TenantId = tenantId,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                    TwoFactorEnabled = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var error = string.Join(" ", createResult.Errors.Select(x => x.Description));
                    await LogHandledAsync(errorLogService, $"Invite registration user creation failed for {maskedEmail}. {error}");
                    return Results.LocalRedirect($"/invite/register?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(error)}");
                }

                try
                {
                    dbContext.Tenants.Add(new Tenant
                    {
                        TenantId = tenantId,
                        Name = BuildTenantName(displayName, normalizedEmail),
                        OwnerUserId = user.Id
                    });

                    await dbContext.SaveChangesAsync();
                    await tenantBootstrapService.SeedDefaultsAsync(tenantId);

                    var acceptResult = await userInvitationService.MarkAcceptedAsync(invitationResult.Data.InvitationId);
                    if (!acceptResult.Success)
                    {
                        await LogHandledAsync(errorLogService, $"Invite registration accept marker failed for {maskedEmail}. {acceptResult.Message}");
                        await userManager.DeleteAsync(user);
                        return Results.LocalRedirect($"/invite/register?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(acceptResult.Message)}");
                    }

                    return Results.LocalRedirect("/login?info=Account%20created%20successfully.%20You%20can%20sign%20in.");
                }
                catch (Exception ex)
                {
                    await errorLogService.LogExceptionAsync(ex, $"Invite registration failed for {maskedEmail}.");
                    await userManager.DeleteAsync(user);
                    return Results.LocalRedirect($"/invite/register?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString("Registration failed. Please try again.")}");
                }
            }).AllowAnonymous();

            authGroup.MapPost("/forgot-password", async (
                [FromForm] string email,
                UserManager<ApplicationUser> userManager,
                IEmailSenderService emailSenderService,
                IVerificationCodeService verificationCodeService,
                IErrorLogService errorLogService) =>
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var maskedEmail = MaskEmail(normalizedEmail);
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null || !await userManager.IsEmailConfirmedAsync(user))
                {
                    await LogHandledAsync(errorLogService, $"Forgot password request ignored for {maskedEmail} because the account is missing or unconfirmed.");
                    return Results.LocalRedirect("/forgot-password?info=If%20the%20account%20exists%2C%20a%20reset%20code%20was%20sent.");
                }

                var codeResult = await verificationCodeService.IssueCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.PasswordReset,
                    expiryMinutes: 10,
                    resendCooldownSeconds: 60,
                    maxAttempts: 3);

                if (!codeResult.Success || codeResult.Data is null)
                {
                    await LogHandledAsync(errorLogService, $"Password reset OTP issue failed for {maskedEmail}. {codeResult.Message}");
                    return Results.LocalRedirect($"/forgot-password?error={Uri.EscapeDataString(codeResult.Message)}");
                }

                var sendResult = await emailSenderService.SendAsync(
                    user.Email!,
                    "MoneyTracker password reset code",
                    $"<p>Your reset code is:</p><h2>{codeResult.Data.Code}</h2><p>Expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

                if (!sendResult.Success)
                {
                    await LogHandledAsync(errorLogService, $"Password reset email failed for {maskedEmail}. {sendResult.Message}");
                    return Results.LocalRedirect($"/forgot-password?error={Uri.EscapeDataString(sendResult.Message)}");
                }

                return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(user.Email!)}&info={Uri.EscapeDataString("A reset code was sent to your email.")}");
            }).AllowAnonymous();

            authGroup.MapPost("/reset-password", async (
                [FromForm] string email,
                [FromForm] string code,
                [FromForm] string newPassword,
                [FromForm] string confirmPassword,
                UserManager<ApplicationUser> userManager,
                IVerificationCodeService verificationCodeService,
                IErrorLogService errorLogService) =>
            {
                if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                {
                    await LogHandledAsync(errorLogService, $"Reset password failed for {email} because confirmation did not match.");
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString("Password confirmation does not match.")}");
                }

                var normalizedEmail = email.Trim().ToLowerInvariant();
                var maskedEmail = MaskEmail(normalizedEmail);
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null)
                {
                    await LogHandledAsync(errorLogService, $"Reset password ignored because user {maskedEmail} was not found.");
                    return Results.LocalRedirect("/login?info=Password%20updated%20if%20the%20account%20exists.");
                }

                var verifyResult = await verificationCodeService.VerifyCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.PasswordReset,
                    code.Replace(" ", string.Empty).Replace("-", string.Empty));

                if (!verifyResult.Success)
                {
                    await LogHandledAsync(errorLogService, $"Reset password OTP verification failed for {maskedEmail}. {verifyResult.Message}");
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString(verifyResult.Message)}");
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, token, newPassword);
                if (!resetResult.Succeeded)
                {
                    var error = string.Join(" ", resetResult.Errors.Select(x => x.Description));
                    await LogHandledAsync(errorLogService, $"Reset password failed for {maskedEmail}. {error}");
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString(error)}");
                }

                return Results.LocalRedirect("/login?info=Password%20updated%20successfully.");
            }).AllowAnonymous();

            authGroup.MapGet("/logout", async (
                HttpRequest request,
                SignInManager<ApplicationUser> signInManager,
                IAuthAuditService authAuditService) =>
            {
                Guid? userId = null;
                Guid? tenantId = null;

                var userIdText = request.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdText, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var tenantIdText = request.HttpContext.User.FindFirst("tenant_id")?.Value;
                if (Guid.TryParse(tenantIdText, out var parsedTenantId))
                {
                    tenantId = parsedTenantId;
                }

                await LogAuthAuditAsync(
                    authAuditService,
                    request,
                    AuthAuditConstants.Logout,
                    true,
                    null,
                    null,
                    userId,
                    tenantId);

                await signInManager.SignOutAsync();
                return Results.LocalRedirect("/login");
            }).AllowAnonymous();

            return app;
        }

        private static string SafeReturnUrl(string? returnUrl)
            => !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') ? returnUrl : "/";

        private static string BuildLoginUrl(string? returnUrl, string? error, string? info)
        {
            var safeReturnUrl = Uri.EscapeDataString(SafeReturnUrl(returnUrl));
            var url = $"/login?returnUrl={safeReturnUrl}";

            if (!string.IsNullOrWhiteSpace(error))
            {
                url += $"&error={Uri.EscapeDataString(error)}";
            }

            if (!string.IsNullOrWhiteSpace(info))
            {
                url += $"&info={Uri.EscapeDataString(info)}";
            }

            return url;
        }

        private static string BuildOtpUrl(string? returnUrl, bool rememberMe, string? error, string? info, DateTime? expiresAtUtc)
        {
            var safeReturnUrl = Uri.EscapeDataString(SafeReturnUrl(returnUrl));
            var url = $"/login-otp?returnUrl={safeReturnUrl}&rememberMe={rememberMe.ToString().ToLowerInvariant()}";

            if (!string.IsNullOrWhiteSpace(error))
            {
                url += $"&error={Uri.EscapeDataString(error)}";
            }

            if (!string.IsNullOrWhiteSpace(info))
            {
                url += $"&info={Uri.EscapeDataString(info)}";
            }

            if (expiresAtUtc.HasValue)
            {
                url += $"&expiresAtUtc={Uri.EscapeDataString(expiresAtUtc.Value.ToString("O"))}";
            }

            return url;
        }

        private static DateTime? ParseUtc(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.TryParse(
                value,
                null,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var parsed)
                ? parsed
                : null;
        }

        private static string BuildTenantName(string? displayName, string normalizedEmail)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            var atIndex = normalizedEmail.IndexOf('@');
            if (atIndex > 0)
            {
                var localPart = normalizedEmail[..atIndex].Trim();
                if (!string.IsNullOrWhiteSpace(localPart))
                {
                    return localPart;
                }
            }

            return "Personal";
        }

        private static Task LogHandledAsync(IErrorLogService errorLogService, string message, CancellationToken cancellationToken = default)
            => errorLogService.LogMessageAsync(message, "Warning", "AuthFlow", cancellationToken);

        private static Task LogAuthAuditAsync(
            IAuthAuditService authAuditService,
            HttpRequest request,
            string action,
            bool success,
            string? failureReason,
            string? emailMasked,
            Guid? userId,
            Guid? tenantId,
            CancellationToken cancellationToken = default)
        {
            var forwardedFor = request.Headers["X-Forwarded-For"].ToString();
            var ipAddress = string.IsNullOrWhiteSpace(forwardedFor)
                ? request.HttpContext.Connection.RemoteIpAddress?.ToString()
                : forwardedFor.Split(',')[0].Trim();

            var entry = new AuthAuditEntryDto
            {
                CreatedAtUtc = DateTime.UtcNow,
                Action = action,
                Outcome = success ? AuthAuditConstants.OutcomeSuccess : AuthAuditConstants.OutcomeFailure,
                FailureReason = failureReason,
                EmailMasked = emailMasked,
                UserId = userId,
                TenantId = tenantId,
                IpAddress = ipAddress,
                UserAgent = request.Headers.UserAgent.ToString(),
                HttpMethod = request.Method,
                Path = request.Path.ToString(),
                TraceId = request.HttpContext.TraceIdentifier
            };

            return authAuditService.LogAsync(entry, cancellationToken);
        }

        private static string? BuildAbsoluteUrl(HttpRequest request, ApplicationSettings settings, IHostEnvironment hostEnvironment, string relativePath)
        {
            if (Uri.TryCreate(relativePath, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            var baseUrl = settings.PublicBaseUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                return $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
            }

            if (!hostEnvironment.IsDevelopment())
            {
                return null;
            }

            var host = request.Host.HasValue ? request.Host.Value : "localhost";
            return $"{request.Scheme}://{host}/{relativePath.TrimStart('/')}";
        }

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "***";
            }

            var atIndex = email.IndexOf('@');
            if (atIndex <= 1)
            {
                return "***";
            }

            return $"{email[0]}***{email[(atIndex - 1)..]}";
        }
    }
}
