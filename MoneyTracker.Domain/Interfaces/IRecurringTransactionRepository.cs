using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IRecurringTransactionRepository
    {
        Task<List<RecurringTransaction>> GetAllAsync();
        Task<RecurringTransaction?> GetByIdAsync(int id);
        Task AddAsync(RecurringTransaction entity);
        Task UpdateAsync(RecurringTransaction entity);
        Task DeleteAsync(int id);
        Task<List<RecurringTransaction>> GetDueAsync(DateTime asOfDateUtc);
    }
}
