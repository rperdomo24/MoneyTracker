using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Interfaces
{
    public interface IUserRegistrationService
    {
        Task<OperationResult> RegisterPublicAsync(string displayName, string email, string password, CancellationToken cancellationToken = default);
        Task<OperationResult> RegisterFromInvitationAsync(string displayName, string password, UserInvitationDetailsDto invitation, CancellationToken cancellationToken = default);
        Task<bool> UserExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    }
}
