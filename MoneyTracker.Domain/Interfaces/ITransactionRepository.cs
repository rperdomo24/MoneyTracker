using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<List<Transaction>> GetAllAsync();
        Task<Transaction?> GetByIdAsync(int id);
        Task AddAsync(Transaction expense);
        Task UpdateAsync(Transaction expense);
        Task DeleteAsync(int id);
    }
}
