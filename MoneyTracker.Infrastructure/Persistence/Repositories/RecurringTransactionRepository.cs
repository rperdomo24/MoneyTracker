using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class RecurringTransactionRepository : IRecurringTransactionRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecurringTransactionRepository> _logger;

        public RecurringTransactionRepository(IServiceScopeFactory scopeFactory, ILogger<RecurringTransactionRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<List<RecurringTransaction>> GetAllAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.RecurringTransactions
                    .AsNoTracking()
                    .Include(r => r.Category)
                    .Include(r => r.Account)
                    .Where(r => !r.IsDeleted)
                    .OrderBy(r => r.NextDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recurring transactions");
                return [];
            }
        }

        public async Task<RecurringTransaction?> GetByIdAsync(int id)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.RecurringTransactions
                    .Include(r => r.Category)
                    .Include(r => r.Account)
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recurring transaction {Id}", id);
                return null;
            }
        }

        public async Task AddAsync(RecurringTransaction entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.RecurringTransactions.Add(entity);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RecurringTransaction entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.RecurringTransactions.Update(entity);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            var entity = await context.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null) return;
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        public async Task<List<RecurringTransaction>> GetDueAsync(DateTime asOfDate)
        {
            try
            {
                var cutoff = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.RecurringTransactions
                    .IgnoreQueryFilters()
                    .Include(r => r.Account)
                    .Include(r => r.Category)
                    .Where(r => !r.IsDeleted && r.IsActive && r.NextDate <= cutoff)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching due recurring transactions");
                return [];
            }
        }
    }
}
