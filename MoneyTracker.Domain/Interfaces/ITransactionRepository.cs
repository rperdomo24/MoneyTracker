using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<List<Transaction>> GetAllAsync();
        Task<Transaction?> GetByIdAsync(int id);
        Task AddAsync(Transaction expense);
        Task UpdateAsync(Transaction expense);
        Task DeleteAsync(int id);
        Task<List<Transaction>> GetFilteredAsync(DateTime? FromDate, DateTime? ToDate, List<int> AccountIds, List<int> TransactionTypeIds);
        Task<int> AddAndReturnIdAsync(Transaction transaction);
        Task<List<Transaction>> GetByCategoryTreeAsync(int categoryId, DateTime fromUtc, DateTime toUtc);
        Task<decimal> GetAccountBalanceAsync(int accountId);
    }
}
