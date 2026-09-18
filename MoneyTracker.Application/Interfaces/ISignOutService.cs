using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Interfaces
{
    public interface ISignOutService
    {
        Task SignOutAsync(Guid? userId, Guid? tenantId, RequestContextDto context, CancellationToken cancellationToken = default);
    }
}
