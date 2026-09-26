using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class GmailConnectionRepository : IGmailConnectionRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GmailConnectionRepository> _logger;

        public GmailConnectionRepository(IServiceScopeFactory scopeFactory, ILogger<GmailConnectionRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<GmailConnection?> GetActiveAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.GmailConnections.FirstOrDefaultAsync(c => !c.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Gmail connection");
                return null;
            }
        }

        public async Task AddAsync(GmailConnection entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.GmailConnections.Add(entity);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(GmailConnection entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.GmailConnections.Update(entity);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            var entity = await context.GmailConnections.FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null) return;
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.AutoSyncEnabled = false;
            await context.SaveChangesAsync();
        }

        public async Task<List<GmailConnection>> GetAllAutoSyncEnabledAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.GmailConnections
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted && c.AutoSyncEnabled)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching auto-sync-enabled Gmail connections");
                return [];
            }
        }

        public async Task<GmailConnection?> GetActiveByTenantAsync(Guid tenantId)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.GmailConnections
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => !c.IsDeleted && c.TenantId == tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Gmail connection for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task UpdateLastSyncByTenantAsync(Guid tenantId, DateTime lastSyncAtUtc)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            await context.GmailConnections
                .IgnoreQueryFilters()
                .Where(c => !c.IsDeleted && c.TenantId == tenantId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.LastSyncAtUtc, lastSyncAtUtc));
        }
    }
}
