using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
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
                SignInManager<ApplicationUser> signInManager,
                UserManager<ApplicationUser> userManager,
                IEmailSenderService emailSenderService,
                IVerificationCodeService verificationCodeService,
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
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, error, null));
                }

                var normalizedEmail = email.Trim().ToLowerInvariant();
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null)
                {
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Invalid email or password.", null));
                }

                var preCheck = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
                if (preCheck.IsLockedOut)
                {
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Your account is temporarily locked. Try again later.", null));
                }

                if (preCheck.IsNotAllowed)
                {
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&error={Uri.EscapeDataString("Email confirmation is required before sign in.")}");
                }

                if (!preCheck.Succeeded)
                {
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Invalid email or password.", null));
                }

                var result = await signInManager.PasswordSignInAsync(
                    user,
                    password,
                    rememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return Results.LocalRedirect(SafeReturnUrl(returnUrl));
                }

                if (!result.RequiresTwoFactor)
                {
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
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, codeResult.Message, null));
                }

                var sendResult = await emailSenderService.SendAsync(
                    user.Email!,
                    "Your MoneyTracker login code",
                    $"<p>Your OTP code is:</p><h2>{codeResult.Data.Code}</h2><p>The code expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

                if (!sendResult.Success)
                {
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, sendResult.Message, null));
                }

                return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, null, "A verification code was sent to your email.", codeResult.Data.ExpiresAtUtc));
            }).AllowAnonymous();

            authGroup.MapPost("/login-otp", async (
                [FromForm] string code,
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                [FromForm] string? expiresAtUtc,
                SignInManager<ApplicationUser> signInManager,
                UserManager<ApplicationUser> userManager,
                IVerificationCodeService verificationCodeService,
                IValidator<LoginOtpRequestDto> validator) =>
            {
                var dto = new LoginOtpRequestDto { Code = code };
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, error, null, ParseUtc(expiresAtUtc)));
                }

                var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user is null)
                {
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Session expired. Please sign in again.", null));
                }

                if (await userManager.IsLockedOutAsync(user))
                {
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
                        return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, "Your account is temporarily locked. Try again later.", null, ParseUtc(expiresAtUtc)));
                    }

                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, "Invalid or expired verification code.", null, ParseUtc(expiresAtUtc)));
                }

                await userManager.ResetAccessFailedCountAsync(user);
                await signInManager.SignInAsync(user, rememberMe);
                return Results.LocalRedirect(SafeReturnUrl(returnUrl));
            }).AllowAnonymous();

            authGroup.MapPost("/login-otp/resend", async (
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                SignInManager<ApplicationUser> signInManager,
                IEmailSenderService emailSenderService,
                IVerificationCodeService verificationCodeService) =>
            {
                var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user is null)
                {
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
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, codeResult.Message, null, null));
                }

                var sendResult = await emailSenderService.SendAsync(
                    user.Email!,
                    "Your MoneyTracker login code",
                    $"<p>Your OTP code is:</p><h2>{codeResult.Data.Code}</h2><p>The code expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

                if (!sendResult.Success)
                {
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, sendResult.Message, null, null));
                }

                return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, null, "A verification code was sent to your email.", codeResult.Data.ExpiresAtUtc));
            }).AllowAnonymous();

            authGroup.MapPost("/resend-confirmation", async (
                [FromForm] string email,
                UserManager<ApplicationUser> userManager,
                IEmailSenderService emailSenderService) =>
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null)
                {
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&info={Uri.EscapeDataString("If the email exists, a confirmation link was sent.")}");
                }

                if (await userManager.IsEmailConfirmedAsync(user))
                {
                    return Results.LocalRedirect(BuildLoginUrl(null, null, "Email already confirmed. You can sign in."));
                }

                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
                var confirmationUrl = $"/confirm-email?userId={Uri.EscapeDataString(user.Id.ToString())}&code={Uri.EscapeDataString(encodedToken)}";
                var sendResult = await emailSenderService.SendAsync(
                    user.Email!,
                    "Confirm your MoneyTracker email",
                    $"<p>Confirm your account:</p><p><a href=\"{confirmationUrl}\">{confirmationUrl}</a></p>");

                if (!sendResult.Success)
                {
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&error={Uri.EscapeDataString(sendResult.Message)}");
                }

                return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&info={Uri.EscapeDataString("Confirmation link sent. Check your email.")}");
            }).AllowAnonymous();

            authGroup.MapPost("/forgot-password", async (
                [FromForm] string email,
                UserManager<ApplicationUser> userManager,
                IEmailSenderService emailSenderService,
                IVerificationCodeService verificationCodeService) =>
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null || !await userManager.IsEmailConfirmedAsync(user))
                {
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
                    return Results.LocalRedirect($"/forgot-password?error={Uri.EscapeDataString(codeResult.Message)}");
                }

                var sendResult = await emailSenderService.SendAsync(
                    user.Email!,
                    "MoneyTracker password reset code",
                    $"<p>Your reset code is:</p><h2>{codeResult.Data.Code}</h2><p>Expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

                if (!sendResult.Success)
                {
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
                IVerificationCodeService verificationCodeService) =>
            {
                if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                {
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString("Password confirmation does not match.")}");
                }

                var normalizedEmail = email.Trim().ToLowerInvariant();
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null)
                {
                    return Results.LocalRedirect("/login?info=Password%20updated%20if%20the%20account%20exists.");
                }

                var verifyResult = await verificationCodeService.VerifyCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.PasswordReset,
                    code.Replace(" ", string.Empty).Replace("-", string.Empty));

                if (!verifyResult.Success)
                {
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString(verifyResult.Message)}");
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, token, newPassword);
                if (!resetResult.Succeeded)
                {
                    var error = string.Join(" ", resetResult.Errors.Select(x => x.Description));
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString(error)}");
                }

                return Results.LocalRedirect("/login?info=Password%20updated%20successfully.");
            }).AllowAnonymous();

            authGroup.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager) =>
            {
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
    }
}
