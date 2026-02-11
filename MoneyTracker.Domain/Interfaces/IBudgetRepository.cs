using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IBudgetRepository
    {
        Task<Budget?> GetByIdAsync(int id);
        Task<Budget?> GetByCategoryMonthAsync(int categoryId, int year, int month);
        Task<List<Budget>> GetByMonthAsync(int year, int month, int? categoryType = null);
        Task AddAsync(Budget budget);
        Task UpdateAsync(Budget budget);
        Task SoftDeleteAsync(int id);
        Task<List<int>> GetCategoryAndChildrenIdsAsync(int categoryId);
    }
}
