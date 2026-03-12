using MoneyTracker.Application.DTOs.Budgets;

namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface IBudgetFilterStateService
    {
        Task SaveAsync(BudgetFilterDto filter);
        Task<BudgetFilterDto?> GetAsync();
        Task ClearAsync();
    }
}
