using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IEmailImportRuleRepository
    {
        // Current-tenant (HTTP request context)
        Task<List<EmailImportRule>> GetAllAsync();
        Task<EmailImportRule?> GetByIdAsync(int id);
        Task AddAsync(EmailImportRule entity);
        Task UpdateAsync(EmailImportRule entity);
        Task DeleteAsync(int id);

        // Cross-tenant (job context)
        Task<List<EmailImportRule>> GetActiveByTenantAsync(Guid tenantId);
    }
}
