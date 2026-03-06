using MoneyTracker.Application.DTOs.Dashboard;

namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface IDashboardFilterStateService
    {
        Task SaveAsync(DashboardFilterDto filter);
        Task<DashboardFilterDto?> GetAsync();
        Task ClearAsync();
    }
}
