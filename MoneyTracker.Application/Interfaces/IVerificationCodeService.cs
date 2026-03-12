using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Interfaces
{
    public interface IVerificationCodeService
    {
        Task<OperationResult<VerificationCodeIssueDto>> IssueCodeAsync(
            Guid userId,
            Guid tenantId,
            string purpose,
            int expiryMinutes,
            int resendCooldownSeconds,
            int maxAttempts = 3);

        Task<OperationResult> VerifyCodeAsync(
            Guid userId,
            Guid tenantId,
            string purpose,
            string code);
    }
}
