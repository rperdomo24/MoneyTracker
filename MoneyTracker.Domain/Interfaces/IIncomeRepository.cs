using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IIncomeRepository
    {
        Task<List<Income>> GetAllAsync();
        Task<Income?> GetByIdAsync(int id);
        Task<bool> AddAsync(Income income);
        Task<bool> UpdateAsync(Income income);
        Task<bool> DeleteAsync(int id);
    }

}
