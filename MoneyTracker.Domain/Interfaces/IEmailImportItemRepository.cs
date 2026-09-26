using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IEmailImportItemRepository
    {
        // Current-tenant (HTTP request context)
        Task<List<EmailImportItem>> GetPendingAsync();
        Task<int> CountPendingAsync();
        Task<EmailImportItem?> GetByIdAsync(int id);
        Task UpdateAsync(EmailImportItem entity);

        // Cross-tenant (job context) — bypass the tenant query filter explicitly
        Task<bool> ExistsByMessageIdAsync(Guid tenantId, string gmailMessageId);
        Task<bool> ExistsByFingerprintAsync(Guid tenantId, string fingerprint);
        Task<bool> ExistsSimilarTransactionAsync(Guid tenantId, decimal amount, DateTime dateUtc, int? accountId);
        Task AddAsync(EmailImportItem entity);
        Task<int> CountPendingByTenantAsync(Guid tenantId);
    }
}
