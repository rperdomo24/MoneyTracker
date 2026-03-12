using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IUserInvitationRepository
    {
        Task<UserInvitation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<UserInvitation?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
        Task<UserInvitation?> GetPendingByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
        Task AddAsync(UserInvitation invitation, CancellationToken cancellationToken = default);
        Task UpdateAsync(UserInvitation invitation, CancellationToken cancellationToken = default);
    }
}
