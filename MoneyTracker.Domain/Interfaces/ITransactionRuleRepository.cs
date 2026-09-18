using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface ITransactionRuleRepository
    {
        Task<List<TransactionRule>> GetAllWithDetailsAsync();
        Task<TransactionRule?> GetByIdWithDetailsAsync(int id);
        Task<List<TransactionRule>> GetEnabledRulesAsync();
        Task AddAsync(TransactionRule rule);
        Task UpdateAsync(TransactionRule rule);
        Task SoftDeleteAsync(int id);
        Task ToggleEnabledAsync(int id, bool enabled);
        Task UpdateOrdersAsync(IEnumerable<(int Id, int Order)> orders);
    }
}
