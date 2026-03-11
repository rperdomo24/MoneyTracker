using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class UserInvitationRepository : IUserInvitationRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<UserInvitationRepository> _logger;

        public UserInvitationRepository(MoneyTrackerDbContext context, ILogger<UserInvitationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserInvitation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.UserInvitations
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invitation {InvitationId}.", id);
                return null;
            }
        }

        public async Task<UserInvitation?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.UserInvitations
                    .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invitation for email {Email}.", normalizedEmail);
                return null;
            }
        }

        public async Task<UserInvitation?> GetPendingByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.UserInvitations
                    .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.AcceptedAtUtc == null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invitation by token hash.");
                return null;
            }
        }

        public async Task AddAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
        {
            _context.UserInvitations.Add(invitation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
        {
            _context.UserInvitations.Update(invitation);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
