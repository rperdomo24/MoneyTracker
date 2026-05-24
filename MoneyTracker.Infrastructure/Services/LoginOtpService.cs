using Microsoft.AspNetCore.Identity;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Services
{
    public class LoginOtpService : ILoginOtpService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IVerificationCodeService _verificationCodeService;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IAuthAuditService _authAuditService;
        private readonly IErrorLogService _errorLogService;

        public LoginOtpService(
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

        public async Task<OperationResult<OtpVerifyResultDto>> VerifyOtpAsync(
            string code,
            bool rememberMe,
            RequestContextDto context,
            CancellationToken cancellationToken = default)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null)
            {
                await LogAsync("OTP verification failed because the auth session expired.", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginOtpVerify, false, "SessionExpired", null, null, null, cancellationToken);
                return OperationResult<OtpVerifyResultDto>.Ok(new OtpVerifyResultDto { SessionExpired = true });
            }

            var maskedEmail = MaskEmail(user.Email);

            if (await _userManager.IsLockedOutAsync(user))
            {
                await LogAsync($"OTP verification failed because user {maskedEmail} is locked out.", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginOtpVerify, false, "LockedOut", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<OtpVerifyResultDto>.Ok(
                    new OtpVerifyResultDto { ErrorMessage = "Your account is temporarily locked. Try again later." });
            }

            var cleanCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var verifyResult = await _verificationCodeService.VerifyCodeAsync(
                user.Id, user.TenantId, AuthVerificationPurposes.LoginOtp, cleanCode);

            if (!verifyResult.Success)
            {
                await _userManager.AccessFailedAsync(user);

                if (await _userManager.IsLockedOutAsync(user))
                {
                    await LogAsync($"OTP verification locked out user {maskedEmail}.", cancellationToken);
                    await AuditAsync(context, AuthAuditConstants.LoginOtpVerify, false, "LockedOutAfterOtpFailure", maskedEmail, user.Id, user.TenantId, cancellationToken);
                    return OperationResult<OtpVerifyResultDto>.Ok(
                        new OtpVerifyResultDto { ErrorMessage = "Your account is temporarily locked. Try again later." });
                }

                await LogAsync($"OTP verification failed for user {maskedEmail}. Invalid or expired code.", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginOtpVerify, false, "InvalidOrExpiredCode", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<OtpVerifyResultDto>.Ok(
                    new OtpVerifyResultDto { ErrorMessage = "Invalid or expired verification code." });
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            await _signInManager.SignInAsync(user, rememberMe);
            await AuditAsync(context, AuthAuditConstants.LoginOtpVerify, true, null, maskedEmail, user.Id, user.TenantId, cancellationToken);

            return OperationResult<OtpVerifyResultDto>.Ok(new OtpVerifyResultDto { SignedIn = true });
        }

        public async Task<OperationResult<OtpResendResultDto>> ResendOtpAsync(
            RequestContextDto context,
            CancellationToken cancellationToken = default)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null)
            {
                await LogAsync("OTP resend failed because the auth session expired.", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginOtpResend, false, "SessionExpired", null, null, null, cancellationToken);
                return OperationResult<OtpResendResultDto>.Ok(new OtpResendResultDto { SessionExpired = true });
            }

            var maskedEmail = MaskEmail(user.Email);

            var codeResult = await _verificationCodeService.IssueCodeAsync(
                user.Id, user.TenantId, AuthVerificationPurposes.LoginOtp,
                expiryMinutes: 10, resendCooldownSeconds: 60, maxAttempts: 3);

            if (!codeResult.Success || codeResult.Data is null)
            {
                await LogAsync($"OTP resend issue failed for user {maskedEmail}. {codeResult.Message}", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginOtpResend, false, "OtpIssueFailed", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<OtpResendResultDto>.Ok(
                    new OtpResendResultDto { ErrorMessage = string.IsNullOrEmpty(codeResult.Message) ? "Failed to issue verification code." : codeResult.Message });
            }

            var sendResult = await _emailSenderService.SendAsync(
                user.Email!,
                "Your MoneyTracker login code",
                $"<p>Your OTP code is:</p><h2>{codeResult.Data.Code}</h2><p>The code expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

            if (!sendResult.Success)
            {
                await LogAsync($"OTP resend email failed for user {maskedEmail}. {sendResult.Message}", cancellationToken);
                await AuditAsync(context, AuthAuditConstants.LoginOtpResend, false, "OtpEmailSendFailed", maskedEmail, user.Id, user.TenantId, cancellationToken);
                return OperationResult<OtpResendResultDto>.Ok(
                    new OtpResendResultDto { ErrorMessage = string.IsNullOrEmpty(sendResult.Message) ? "Failed to send verification email." : sendResult.Message });
            }

            await AuditAsync(context, AuthAuditConstants.LoginOtpResend, true, null, maskedEmail, user.Id, user.TenantId, cancellationToken);
            return OperationResult<OtpResendResultDto>.Ok(
                new OtpResendResultDto { OtpExpiresAtUtc = codeResult.Data.ExpiresAtUtc });
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
