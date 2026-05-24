using Microsoft.AspNetCore.Identity;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Services
{
    public class LoginService : ILoginService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IVerificationCodeService _verificationCodeService;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IAuthAuditService _authAuditService;
        private readonly IErrorLogService _errorLogService;

        public LoginService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IVerificationCodeService verificationCodeService,
            IEmailSenderService emailSenderService,
            IAuthAuditService authAuditService,
            IErrorLogService errorLogService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _verificationCodeService = verificationCodeService;
            _emailSenderService = emailSenderService;
            _authAuditService = authAuditService;
            _errorLogService = errorLogService;
        }

        public async Task<OperationResult<PasswordLoginResultDto>> LoginAsync(
            string email,
            string password,
            bool rememberMe,
            RequestContextDto context,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var maskedEmail = MaskEmail(normalizedEmail);

            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user is null)
            {
                await LogAsync($"Login failed because user {maskedEmail} was not found.", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginPassword, false, "UserNotFound", maskedEmail, null, null, cancellationToken);
                return OperationResult<PasswordLoginResultDto>.Ok(
                    new PasswordLoginResultDto { ErrorMessage = "Invalid email or password." });
            }

            var preCheck = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (preCheck.IsLockedOut)
            {
                await LogAsync($"Login failed because user {maskedEmail} is locked out.", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginPassword, false, "LockedOut", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<PasswordLoginResultDto>.Ok(
                    new PasswordLoginResultDto { ErrorMessage = "Your account is temporarily locked. Try again later." });
            }

            if (preCheck.IsNotAllowed)
            {
                await LogAsync($"Login blocked because user {maskedEmail} has not confirmed email.", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginPassword, false, "EmailNotConfirmed", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<PasswordLoginResultDto>.Ok(
                    new PasswordLoginResultDto { EmailNotConfirmed = true, EmailForPendingPage = normalizedEmail });
            }

            if (!preCheck.Succeeded)
            {
                await LogAsync($"Login failed because password validation did not succeed for {maskedEmail}.", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginPassword, false, "InvalidPassword", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<PasswordLoginResultDto>.Ok(
                    new PasswordLoginResultDto { ErrorMessage = "Invalid email or password." });
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                await AuditAsync(context, AuthAuditConstants.LoginPassword, true, null, maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<PasswordLoginResultDto>.Ok(
                    new PasswordLoginResultDto { SignedIn = true });
            }

            if (!result.RequiresTwoFactor)
            {
                await LogAsync($"Login failed because sign-in did not complete for {maskedEmail}.", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginPassword, false, "SignInIncomplete", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<PasswordLoginResultDto>.Ok(
                    new PasswordLoginResultDto { ErrorMessage = "Invalid email or password." });
            }

            var codeResult = await _verificationCodeService.IssueCodeAsync(
                user.Id, user.TenantId, AuthVerificationPurposes.LoginOtp,
                expiryMinutes: 10, resendCooldownSeconds: 60, maxAttempts: 3);

            if (!codeResult.Success || codeResult.Data is null)
            {
                await LogAsync($"OTP issue failed during login for {maskedEmail}. {codeResult.Message}", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginPassword, false, "OtpIssueFailed", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<PasswordLoginResultDto>.Ok(
                    new PasswordLoginResultDto { ErrorMessage = string.IsNullOrEmpty(codeResult.Message) ? "Failed to issue verification code." : codeResult.Message });
            }

            var sendResult = await _emailSenderService.SendAsync(
                user.Email!,
                "Your MoneyTracker login code",
                $"<p>Your OTP code is:</p><h2>{codeResult.Data.Code}</h2><p>The code expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

            if (!sendResult.Success)
            {
                await LogAsync($"OTP email send failed during login for {maskedEmail}. {sendResult.Message}", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginPassword, false, "OtpEmailSendFailed", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<PasswordLoginResultDto>.Ok(
                    new PasswordLoginResultDto { ErrorMessage = string.IsNullOrEmpty(sendResult.Message) ? "Failed to send verification email." : sendResult.Message });
            }

            return OperationResult<PasswordLoginResultDto>.Ok(
                new PasswordLoginResultDto { RequiresOtp = true, OtpExpiresAtUtc = codeResult.Data.ExpiresAtUtc });
        }

        private Task LogAsync(string message, CancellationToken ct)
            => _errorLogService.LogMessageAsync(message, "Warning", "AuthFlow", ct);

        private Task AuditAsync(RequestContextDto context, string action, bool success, string? reason,
            string? maskedEmail, Guid? userId, Guid? tenantId, CancellationToken ct)
        {
            return _authAuditService.LogAsync(new AuthAuditEntryDto
            {
                CreatedAtUtc = DateTime.UtcNow,
                Action = action,
                Outcome = success ? AuthAuditConstants.OutcomeSuccess : AuthAuditConstants.OutcomeFailure,
                FailureReason = reason,
                EmailMasked = maskedEmail,
                UserId = userId,
                TenantId = tenantId,
                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,
                HttpMethod = context.HttpMethod,
                Path = context.Path,
                TraceId = context.TraceId
            }, ct);
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
