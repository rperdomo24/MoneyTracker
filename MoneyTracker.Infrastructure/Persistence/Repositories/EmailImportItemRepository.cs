using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class EmailImportItemRepository : IEmailImportItemRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailImportItemRepository> _logger;

        public EmailImportItemRepository(IServiceScopeFactory scopeFactory, ILogger<EmailImportItemRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<List<EmailImportItem>> GetPendingAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.EmailImportItems
                    .AsNoTracking()
                    .Where(i => i.Status == EmailImportStatus.Pending)
                    .OrderByDescending(i => i.ReceivedAtUtc)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending email import items");
                return [];
            }
        }

        public async Task<int> CountPendingAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.EmailImportItems.CountAsync(i => i.Status == EmailImportStatus.Pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting pending email import items");
                return 0;
            }
        }

        public async Task<EmailImportItem?> GetByIdAsync(int id)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.EmailImportItems.FirstOrDefaultAsync(i => i.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching email import item {Id}", id);
                return null;
            }
        }

        public async Task UpdateAsync(EmailImportItem entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.EmailImportItems.Update(entity);
            await context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByMessageIdAsync(Guid tenantId, string gmailMessageId)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.EmailImportItems
                    .IgnoreQueryFilters()
                    .AnyAsync(i => i.TenantId == tenantId && i.GmailMessageId == gmailMessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existing Gmail message id {MessageId}", gmailMessageId);
                return true; // fail safe: skip the message rather than risk re-processing
            }
        }

        public async Task<bool> ExistsByFingerprintAsync(Guid tenantId, string fingerprint)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.EmailImportItems
                    .IgnoreQueryFilters()
                    .AnyAsync(i => i.TenantId == tenantId && i.Fingerprint == fingerprint
                        && i.Status != EmailImportStatus.Dismissed && i.Status != EmailImportStatus.Duplicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking fingerprint {Fingerprint}", fingerprint);
                return false;
            }
        }

        public async Task<bool> ExistsSimilarTransactionAsync(Guid tenantId, decimal amount, DateTime dateUtc, int? accountId)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                var from = dateUtc.Date.AddDays(-1);
                var to = dateUtc.Date.AddDays(2);

                return await context.Transaction
                    .IgnoreQueryFilters()
                    .AnyAsync(t => t.TenantId == tenantId
                        && !t.IsDeleted
                        && Math.Abs(t.Amount - amount) < 0.01m
                        && t.Date >= from && t.Date < to
                        && (!accountId.HasValue || t.AccountId == accountId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking similar transactions for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task AddAsync(EmailImportItem entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.EmailImportItems.Add(entity);
            await context.SaveChangesAsync();
        }

        public async Task<int> CountPendingByTenantAsync(Guid tenantId)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.EmailImportItems
                    .IgnoreQueryFilters()
                    .CountAsync(i => i.TenantId == tenantId && !i.IsDeleted && i.Status == EmailImportStatus.Pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting pending email import items for tenant {TenantId}", tenantId);
                return 0;
            }
        }
    }
}
