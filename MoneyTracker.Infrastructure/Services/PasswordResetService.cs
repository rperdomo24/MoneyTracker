using Microsoft.AspNetCore.Identity;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVerificationCodeService _verificationCodeService;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IErrorLogService _errorLogService;

        public PasswordResetService(
            UserManager<ApplicationUser> userManager,
            IVerificationCodeService verificationCodeService,
            IEmailSenderService emailSenderService,
            IErrorLogService errorLogService)
        {
            _userManager = userManager;
            _verificationCodeService = verificationCodeService;
            _emailSenderService = emailSenderService;
            _errorLogService = errorLogService;
        }

        public async Task<OperationResult<bool>> InitiateResetAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            var maskedEmail = MaskEmail(normalizedEmail);
            var user = await _userManager.FindByEmailAsync(normalizedEmail);

            if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                await _errorLogService.LogMessageAsync(
                    $"Forgot password request ignored for {maskedEmail} because the account is missing or unconfirmed.",
                    "Warning", "AuthFlow", cancellationToken);
                return OperationResult<bool>.Ok(false);
            }

            var codeResult = await _verificationCodeService.IssueCodeAsync(
                user.Id, user.TenantId, AuthVerificationPurposes.PasswordReset,
                expiryMinutes: 10, resendCooldownSeconds: 60, maxAttempts: 3);

            if (!codeResult.Success || codeResult.Data is null)
            {
                await _errorLogService.LogMessageAsync(
                    $"Password reset OTP issue failed for {maskedEmail}. {codeResult.Message}",
                    "Warning", "AuthFlow", cancellationToken);
                return OperationResult<bool>.Fail(string.IsNullOrEmpty(codeResult.Message) ? "Failed to issue reset code." : codeResult.Message);
            }

            var sendResult = await _emailSenderService.SendAsync(
                user.Email!,
                "MoneyTracker password reset code",
                $"<p>Your reset code is:</p><h2>{codeResult.Data.Code}</h2><p>Expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

            if (!sendResult.Success)
            {
                await _errorLogService.LogMessageAsync(
                    $"Password reset email failed for {maskedEmail}. {sendResult.Message}",
                    "Warning", "AuthFlow", cancellationToken);
                return OperationResult<bool>.Fail(string.IsNullOrEmpty(sendResult.Message) ? "Failed to send reset email." : sendResult.Message);
            }

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> ResetPasswordAsync(
            string email,
            string code,
            string newPassword,
            string confirmPassword,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                await _errorLogService.LogMessageAsync(
                    $"Reset password failed for {MaskEmail(email)} because confirmation did not match.",
                    "Warning", "AuthFlow", cancellationToken);
                return OperationResult<bool>.Fail("Password confirmation does not match.");
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var maskedEmail = MaskEmail(normalizedEmail);
            var user = await _userManager.FindByEmailAsync(normalizedEmail);

            if (user is null)
            {
                await _errorLogService.LogMessageAsync(
                    $"Reset password ignored because user {maskedEmail} was not found.",
                    "Warning", "AuthFlow", cancellationToken);
                return OperationResult<bool>.Ok(false);
            }

            var cleanCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var verifyResult = await _verificationCodeService.VerifyCodeAsync(
                user.Id, user.TenantId, AuthVerificationPurposes.PasswordReset, cleanCode);

            if (!verifyResult.Success)
            {
                await _errorLogService.LogMessageAsync(
                    $"Reset password OTP verification failed for {maskedEmail}. {verifyResult.Message}",
                    "Warning", "AuthFlow", cancellationToken);
                return OperationResult<bool>.Fail(string.IsNullOrEmpty(verifyResult.Message) ? "Invalid or expired code." : verifyResult.Message);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!resetResult.Succeeded)
            {
                var error = string.Join(" ", resetResult.Errors.Select(x => x.Description));
                await _errorLogService.LogMessageAsync(
                    $"Reset password failed for {maskedEmail}. {error}",
                    "Warning", "AuthFlow", cancellationToken);
                return OperationResult<bool>.Fail(error);
            }

            return OperationResult<bool>.Ok(true);
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
