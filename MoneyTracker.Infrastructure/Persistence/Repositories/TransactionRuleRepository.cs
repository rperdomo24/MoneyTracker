using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class TransactionRuleRepository : ITransactionRuleRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<TransactionRuleRepository> _logger;

        public TransactionRuleRepository(MoneyTrackerDbContext context, ILogger<TransactionRuleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TransactionRule>> GetAllWithDetailsAsync()
        {
            try
            {
                return await _context.TransactionRules
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted)
                    .Include(r => r.Conditions)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.Merchant)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.Category)
                    .OrderBy(r => r.Order)
                    .ThenBy(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all transaction rules.");
                return new();
            }
        }

        public async Task<TransactionRule?> GetByIdWithDetailsAsync(int id)
        {
            try
            {
                return await _context.TransactionRules
                    .Where(r => r.Id == id && !r.IsDeleted)
                    .Include(r => r.Conditions)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.Merchant)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.Category)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rule by ID {Id}.", id);
                return null;
            }
        }

        public async Task<List<TransactionRule>> GetEnabledRulesAsync()
        {
            try
            {
                return await _context.TransactionRules
                    .Where(r => !r.IsDeleted && r.IsEnabled)
                    .Include(r => r.Conditions)
                    .Include(r => r.Actions)
                    .OrderBy(r => r.Order)
                    .ThenBy(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled rules.");
                return new();
            }
        }

        public async Task AddAsync(TransactionRule rule)
        {
            try
            {
                _context.TransactionRules.Add(rule);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding transaction rule.");
                throw;
            }
        }

        public async Task UpdateAsync(TransactionRule rule)
        {
            try
            {
                var existing = await _context.TransactionRules
                    .Include(r => r.Conditions)
                    .Include(r => r.Actions)
                    .FirstOrDefaultAsync(r => r.Id == rule.Id);

                if (existing is null) return;

                existing.Name = rule.Name;
                existing.IsEnabled = rule.IsEnabled;
                existing.ApplyToHistorical = rule.ApplyToHistorical;
                existing.ApplyFromDate = rule.ApplyFromDate;
                existing.Order = rule.Order;
                existing.UpdatedAt = rule.UpdatedAt;

                _context.TransactionRuleConditions.RemoveRange(existing.Conditions);
                _context.TransactionRuleActions.RemoveRange(existing.Actions);

                foreach (var cond in rule.Conditions)
                {
                    cond.Id = 0;
                    cond.RuleId = rule.Id;
                    _context.TransactionRuleConditions.Add(cond);
                }
                foreach (var action in rule.Actions)
                {
                    action.Id = 0;
                    action.RuleId = rule.Id;
                    action.Merchant = null;
                    action.Category = null;
                    _context.TransactionRuleActions.Add(action);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction rule.");
                throw;
            }
        }

        public async Task SoftDeleteAsync(int id)
        {
            try
            {
                var entity = await _context.TransactionRules.FindAsync(id);
                if (entity is null) return;

                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting transaction rule with ID {Id}.", id);
                throw;
            }
        }

        public async Task ToggleEnabledAsync(int id, bool enabled)
        {
            try
            {
                var entity = await _context.TransactionRules.FindAsync(id);
                if (entity is null) return;

                entity.IsEnabled = enabled;
                entity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling rule enabled state.");
                throw;
            }
        }

        public async Task UpdateOrdersAsync(IEnumerable<(int Id, int Order)> orders)
        {
            try
            {
                var now = DateTime.UtcNow;
                foreach (var (id, order) in orders)
                {
                    var entity = await _context.TransactionRules.FindAsync(id);
                    if (entity is null) continue;
                    entity.Order = order;
                    entity.UpdatedAt = now;
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rule orders.");
                throw;
            }
        }
    }
}
