using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class EmailImportRuleRepository : IEmailImportRuleRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailImportRuleRepository> _logger;

        public EmailImportRuleRepository(IServiceScopeFactory scopeFactory, ILogger<EmailImportRuleRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<List<EmailImportRule>> GetAllAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.EmailImportRules
                    .AsNoTracking()
                    .OrderBy(r => r.BankLabel)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching email import rules");
                return [];
            }
        }

        public async Task<EmailImportRule?> GetByIdAsync(int id)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.EmailImportRules.FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching email import rule {Id}", id);
                return null;
            }
        }

        public async Task AddAsync(EmailImportRule entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.EmailImportRules.Add(entity);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EmailImportRule entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.EmailImportRules.Update(entity);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            var entity = await context.EmailImportRules.FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null) return;
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        public async Task<List<EmailImportRule>> GetActiveByTenantAsync(Guid tenantId)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.EmailImportRules
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted && r.TenantId == tenantId && r.IsActive)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active email import rules for tenant {TenantId}", tenantId);
                return [];
            }
        }
    }
}
