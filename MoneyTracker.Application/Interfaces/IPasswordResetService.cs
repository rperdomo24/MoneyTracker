using MoneyTracker.Application.Common;

namespace MoneyTracker.Application.Interfaces
{
    public interface IPasswordResetService
    {
        /// <summary>Returns Ok(true) if code sent, Ok(false) if user not found/ineligible (privacy), Fail on system error.</summary>
        Task<OperationResult<bool>> InitiateResetAsync(string normalizedEmail, CancellationToken cancellationToken = default);

        /// <summary>Returns Ok(true) on success, Ok(false) if user not found (privacy), Fail with message on validation or identity error.</summary>
        Task<OperationResult<bool>> ResetPasswordAsync(
            string email,
            string code,
            string newPassword,
            string confirmPassword,
            CancellationToken cancellationToken = default);
    }
}
