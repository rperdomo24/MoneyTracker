using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class SavingsGoalRepository : ISavingsGoalRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<SavingsGoalRepository> _logger;

        public SavingsGoalRepository(MoneyTrackerDbContext context, ILogger<SavingsGoalRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<SavingsGoal>> GetAllAsync()
        {
            try
            {
                return await _context.SavingsGoals
                    .AsNoTracking()
                    .Include(g => g.Contributions)
                        .ThenInclude(c => c.LinkedTransaction)
                            .ThenInclude(t => t!.Account)
                    .OrderByDescending(g => g.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all savings goals.");
                return new();
            }
        }

        public async Task<SavingsGoal?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.SavingsGoals
                    .Include(g => g.Contributions)
                        .ThenInclude(c => c.LinkedTransaction)
                            .ThenInclude(t => t!.Account)
                    .FirstOrDefaultAsync(g => g.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting savings goal by ID {Id}.", id);
                return null;
            }
        }

        public async Task AddAsync(SavingsGoal goal)
        {
            try
            {
                _context.SavingsGoals.Add(goal);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding savings goal.");
                throw;
            }
        }

        public async Task UpdateAsync(SavingsGoal goal)
        {
            try
            {
                _context.SavingsGoals.Update(goal);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating savings goal.");
                throw;
            }
        }

        public async Task SoftDeleteAsync(int id)
        {
            try
            {
                var entity = await _context.SavingsGoals.FindAsync(id);
                if (entity is null) return;

                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting savings goal with ID {Id}.", id);
                throw;
            }
        }

        public async Task AddContributionAsync(SavingsContribution contribution)
        {
            try
            {
                _context.SavingsContributions.Add(contribution);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding savings contribution.");
                throw;
            }
        }

        public async Task UpdateContributionAsync(SavingsContribution contribution)
        {
            try
            {
                _context.SavingsContributions.Update(contribution);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating savings contribution with ID {Id}.", contribution.Id);
                throw;
            }
        }

        public async Task DeleteContributionAsync(int contributionId)
        {
            try
            {
                var entity = await _context.SavingsContributions.FindAsync(contributionId);
                if (entity is null) return;

                _context.SavingsContributions.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting savings contribution with ID {Id}.", contributionId);
                throw;
            }
        }

        public async Task<SavingsContribution?> GetContributionByIdAsync(int contributionId)
        {
            try
            {
                return await _context.SavingsContributions.FindAsync(contributionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting savings contribution with ID {Id}.", contributionId);
                return null;
            }
        }
    }
}
