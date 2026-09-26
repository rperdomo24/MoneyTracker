using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IGmailConnectionRepository
    {
        // Current-tenant (HTTP request context)
        Task<GmailConnection?> GetActiveAsync();
        Task AddAsync(GmailConnection entity);
        Task UpdateAsync(GmailConnection entity);
        Task DeleteAsync(int id);

        // Cross-tenant (job context) — bypass the tenant query filter explicitly
        Task<List<GmailConnection>> GetAllAutoSyncEnabledAsync();
        Task<GmailConnection?> GetActiveByTenantAsync(Guid tenantId);
        Task UpdateLastSyncByTenantAsync(Guid tenantId, DateTime lastSyncAtUtc);
    }
}
