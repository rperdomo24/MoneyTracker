using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<List<Transaction>> GetAllAsync();
        Task<Transaction?> GetByIdAsync(int id);
        Task AddAsync(Transaction expense);
        Task UpdateAsync(Transaction expense);
        Task DeleteAsync(int id);
        Task<List<Transaction>> GetFilteredAsync(
            DateTime? fromDate,
            DateTime? toDate,
            List<int> accountIds,
            List<int> transactionTypeIds,
            bool skipSorting = false);
        Task<List<TransactionTrendEntry>> GetTrendEntriesAsync(
            DateTime? fromDateUtc,
            DateTime? toDateUtc,
            List<int> accountIds,
            CategoryTypeEnum? categoryType = null);
        Task<int> AddAndReturnIdAsync(Transaction transaction);
        Task<List<Transaction>> GetByCategoryTreeAsync(int categoryId, DateTime fromUtc, DateTime toUtc);
        Task<decimal> GetAccountBalanceAsync(int accountId);
        Task<List<int>> SoftDeleteByAccountAsync(int accountId, CancellationToken cancellationToken = default);
    }
}
