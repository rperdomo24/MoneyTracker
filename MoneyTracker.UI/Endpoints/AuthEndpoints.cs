using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;

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
                ILoginService loginService,
                IErrorLogService errorLogService,
                IAuthAuditService authAuditService,
                IValidator<LoginRequestDto> validator) =>
            {
                var dto = new LoginRequestDto { Email = email, Password = password, RememberMe = rememberMe };
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
                    await LogHandledAsync(errorLogService, $"Login validation failed. {error}");
                    await LogAuthAuditAsync(authAuditService, request, AuthAuditConstants.LoginPassword, false, "ValidationFailed", null, null, null);
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, error, null));
                }

                var result = await loginService.LoginAsync(email, password, rememberMe, ExtractContext(request));
                if (!result.Success)
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "An unexpected error occurred. Please try again.", null));

                var data = result.Data!;
                if (data.EmailNotConfirmed)
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(data.EmailForPendingPage ?? string.Empty)}&error={Uri.EscapeDataString("Email confirmation is required before sign in.")}");
                if (data.RequiresOtp)
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, null, "A verification code was sent to your email.", data.OtpExpiresAtUtc));
                if (data.SignedIn)
                    return Results.LocalRedirect(SafeReturnUrl(returnUrl));

                return Results.LocalRedirect(BuildLoginUrl(returnUrl, data.ErrorMessage, null));
            }).AllowAnonymous();

            authGroup.MapPost("/login-otp", async (
                [FromForm] string code,
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                [FromForm] string? expiresAtUtc,
                HttpRequest request,
                ILoginOtpService loginOtpService,
                IErrorLogService errorLogService,
                IAuthAuditService authAuditService,
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

                var result = await loginOtpService.VerifyOtpAsync(code, rememberMe, ExtractContext(request));
                if (!result.Success)
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, "An unexpected error occurred.", null, ParseUtc(expiresAtUtc)));

                var data = result.Data!;
                if (data.SessionExpired)
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Session expired. Please sign in again.", null));
                if (data.SignedIn)
                    return Results.LocalRedirect(SafeReturnUrl(returnUrl));

                return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, data.ErrorMessage, null, ParseUtc(expiresAtUtc)));
            }).AllowAnonymous();

            authGroup.MapPost("/login-otp/resend", async (
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                HttpRequest request,
                ILoginOtpService loginOtpService) =>
            {
                var result = await loginOtpService.ResendOtpAsync(ExtractContext(request));
                if (!result.Success)
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, "An unexpected error occurred.", null, null));

                var data = result.Data!;
                if (data.SessionExpired)
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Session expired. Please sign in again.", null));
                if (data.ErrorMessage is not null)
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, data.ErrorMessage, null, null));

                return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, null, "A verification code was sent to your email.", data.OtpExpiresAtUtc));
            }).AllowAnonymous();

            authGroup.MapPost("/resend-confirmation", async (
                [FromForm] string email,
                HttpRequest request,
                IEmailConfirmationService emailConfirmationService,
                IErrorLogService errorLogService,
                IOptions<ApplicationSettings> applicationOptions,
                IHostEnvironment hostEnvironment) =>
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var baseUrl = DetermineBaseUrl(request, applicationOptions.Value, hostEnvironment);
                if (baseUrl is null)
                {
                    await LogHandledAsync(errorLogService, "Confirmation email could not be prepared because PublicBaseUrl is missing.");
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&error={Uri.EscapeDataString("PublicBaseUrl must be configured before sending confirmation emails.")}");
                }

                var result = await emailConfirmationService.ResendConfirmationEmailAsync(normalizedEmail, baseUrl);
                if (!result.Success)
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&error={Uri.EscapeDataString(result.Message)}");

                if (result.Data == true)
                    return Results.LocalRedirect(BuildLoginUrl(null, null, "Email already confirmed. You can sign in."));

                return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&info={Uri.EscapeDataString("If the email exists, a confirmation link was sent.")}");
            }).AllowAnonymous();

            authGroup.MapPost("/register", async (
                [FromForm] string displayName,
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] string confirmPassword,
                IUserRegistrationService userRegistrationService,
                IErrorLogService errorLogService,
                IOptions<ApplicationSettings> applicationOptions,
                IValidator<RegisterRequestDto> validator) =>
            {
                if (!applicationOptions.Value.EnablePublicRegistration)
                {
                    await LogHandledAsync(errorLogService, "Public registration attempt rejected because it is disabled by configuration.");
                    return Results.LocalRedirect(BuildRegisterUrl(null, null, "Public registration is currently disabled.", null));
                }

                var dto = new RegisterRequestDto { DisplayName = displayName, Email = email, Password = password, ConfirmPassword = confirmPassword };
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
                    await LogHandledAsync(errorLogService, $"Public registration validation failed. {error}");
                    return Results.LocalRedirect(BuildRegisterUrl(displayName, email, error, null));
                }

                var normalizedEmail = email.Trim().ToLowerInvariant();
                if (await userRegistrationService.UserExistsAsync(normalizedEmail))
                {
                    await LogHandledAsync(errorLogService, $"Public registration skipped because {MaskEmail(normalizedEmail)} already has an account.");
                    return Results.LocalRedirect($"/login?info={Uri.EscapeDataString("This email already has an account. Please sign in.")}");
                }

                var result = await userRegistrationService.RegisterPublicAsync(displayName, normalizedEmail, password);
                if (!result.Success)
                    return Results.LocalRedirect(BuildRegisterUrl(displayName, email, result.Message, null));

                return Results.LocalRedirect("/login?info=Account%20created%20successfully.%20You%20can%20sign%20in.");
            }).AllowAnonymous();

            authGroup.MapPost("/invite-register", async (
                [FromForm] string token,
                [FromForm] string displayName,
                [FromForm] string password,
                [FromForm] string confirmPassword,
                IUserRegistrationService userRegistrationService,
                IUserInvitationService userInvitationService,
                IErrorLogService errorLogService,
                IValidator<AcceptInvitationRequestDto> validator) =>
            {
                var dto = new AcceptInvitationRequestDto { Token = token, DisplayName = displayName, Password = password, ConfirmPassword = confirmPassword };
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
                if (await userRegistrationService.UserExistsAsync(normalizedEmail))
                {
                    await LogHandledAsync(errorLogService, $"Invite registration skipped because {MaskEmail(normalizedEmail)} already has an account.");
                    return Results.LocalRedirect($"/login?info={Uri.EscapeDataString("This invitation email already has an account. Please sign in.")}");
                }

                var result = await userRegistrationService.RegisterFromInvitationAsync(displayName, password, invitationResult.Data);
                if (!result.Success)
                    return Results.LocalRedirect($"/invite/register?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(result.Message)}");

                return Results.LocalRedirect("/login?info=Account%20created%20successfully.%20You%20can%20sign%20in.");
            }).AllowAnonymous();

            authGroup.MapPost("/forgot-password", async (
                [FromForm] string email,
                IPasswordResetService passwordResetService) =>
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var result = await passwordResetService.InitiateResetAsync(normalizedEmail);

                if (!result.Success)
                    return Results.LocalRedirect($"/forgot-password?error={Uri.EscapeDataString(result.Message)}");

                if (result.Data != true)
                    return Results.LocalRedirect("/forgot-password?info=If%20the%20account%20exists%2C%20a%20reset%20code%20was%20sent.");

                return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(normalizedEmail)}&info={Uri.EscapeDataString("A reset code was sent to your email.")}");
            }).AllowAnonymous();

            authGroup.MapPost("/reset-password", async (
                [FromForm] string email,
                [FromForm] string code,
                [FromForm] string newPassword,
                [FromForm] string confirmPassword,
                IPasswordResetService passwordResetService) =>
            {
                var result = await passwordResetService.ResetPasswordAsync(email, code, newPassword, confirmPassword);

                if (!result.Success)
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString(result.Message)}");

                if (result.Data != true)
                    return Results.LocalRedirect("/login?info=Password%20updated%20if%20the%20account%20exists.");

                return Results.LocalRedirect("/login?info=Password%20updated%20successfully.");
            }).AllowAnonymous();

            authGroup.MapGet("/logout", async (
                HttpRequest request,
                ISignOutService signOutService) =>
            {
                Guid? userId = null;
                Guid? tenantId = null;

                var userIdText = request.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdText, out var parsedUserId))
                    userId = parsedUserId;

                var tenantIdText = request.HttpContext.User.FindFirst("tenant_id")?.Value;
                if (Guid.TryParse(tenantIdText, out var parsedTenantId))
                    tenantId = parsedTenantId;

                await signOutService.SignOutAsync(userId, tenantId, ExtractContext(request));
                return Results.LocalRedirect("/login");
            }).AllowAnonymous();

            return app;
        }

        private static RequestContextDto ExtractContext(HttpRequest request)
        {
            var forwardedFor = request.Headers["X-Forwarded-For"].ToString();
            var ipAddress = string.IsNullOrWhiteSpace(forwardedFor)
                ? request.HttpContext.Connection.RemoteIpAddress?.ToString()
                : forwardedFor.Split(',')[0].Trim();

            return new RequestContextDto
            {
                IpAddress = ipAddress,
                UserAgent = request.Headers.UserAgent.ToString(),
                HttpMethod = request.Method,
                Path = request.Path.ToString(),
                TraceId = request.HttpContext.TraceIdentifier
            };
        }

        private static string SafeReturnUrl(string? returnUrl)
            => !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') ? returnUrl : "/";

        private static string BuildLoginUrl(string? returnUrl, string? error, string? info)
        {
            var safeReturnUrl = Uri.EscapeDataString(SafeReturnUrl(returnUrl));
            var url = $"/login?returnUrl={safeReturnUrl}";

            if (!string.IsNullOrWhiteSpace(error))
                url += $"&error={Uri.EscapeDataString(error)}";

            if (!string.IsNullOrWhiteSpace(info))
                url += $"&info={Uri.EscapeDataString(info)}";

            return url;
        }

        private static string BuildOtpUrl(string? returnUrl, bool rememberMe, string? error, string? info, DateTime? expiresAtUtc)
        {
            var safeReturnUrl = Uri.EscapeDataString(SafeReturnUrl(returnUrl));
            var url = $"/login-otp?returnUrl={safeReturnUrl}&rememberMe={rememberMe.ToString().ToLowerInvariant()}";

            if (!string.IsNullOrWhiteSpace(error))
                url += $"&error={Uri.EscapeDataString(error)}";

            if (!string.IsNullOrWhiteSpace(info))
                url += $"&info={Uri.EscapeDataString(info)}";

            if (expiresAtUtc.HasValue)
                url += $"&expiresAtUtc={Uri.EscapeDataString(expiresAtUtc.Value.ToString("O"))}";

            return url;
        }

        private static string BuildRegisterUrl(string? displayName, string? email, string? error, string? info)
        {
            var url = "/register";
            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(displayName))
                query.Add($"displayName={Uri.EscapeDataString(displayName)}");

            if (!string.IsNullOrWhiteSpace(email))
                query.Add($"email={Uri.EscapeDataString(email)}");

            if (!string.IsNullOrWhiteSpace(error))
                query.Add($"error={Uri.EscapeDataString(error)}");

            if (!string.IsNullOrWhiteSpace(info))
                query.Add($"info={Uri.EscapeDataString(info)}");

            return query.Count == 0 ? url : $"{url}?{string.Join("&", query)}";
        }

        private static DateTime? ParseUtc(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return DateTime.TryParse(
                value,
                null,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var parsed)
                ? parsed
                : null;
        }

        private static string? DetermineBaseUrl(HttpRequest request, ApplicationSettings settings, IHostEnvironment hostEnvironment)
        {
            var baseUrl = settings.PublicBaseUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(baseUrl))
                return baseUrl.TrimEnd('/');

            if (!hostEnvironment.IsDevelopment())
                return null;

            var host = request.Host.HasValue ? request.Host.Value : "localhost";
            return $"{request.Scheme}://{host}";
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

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "***";
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1) return "***";
            return $"{email[0]}***{email[(atIndex - 1)..]}";
        }
    }
}
