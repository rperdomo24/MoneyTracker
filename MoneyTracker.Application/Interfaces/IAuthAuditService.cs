using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Interfaces
{
    public interface IAuthAuditService
    {
        Task LogAsync(AuthAuditEntryDto entry, CancellationToken cancellationToken = default);
    }
}
