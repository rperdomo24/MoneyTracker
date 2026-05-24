using MoneyTracker.Application.Common;

namespace MoneyTracker.Application.Interfaces
{
    public interface IEmailConfirmationService
    {
        Task<OperationResult> ConfirmEmailAsync(string? userId, string? code, CancellationToken cancellationToken = default);

        /// <summary>Returns Ok(true) if already confirmed, Ok(false) if sent or user not found (privacy), Fail on send error.</summary>
        Task<OperationResult<bool>> ResendConfirmationEmailAsync(string normalizedEmail, string publicBaseUrl, CancellationToken cancellationToken = default);
    }
}
