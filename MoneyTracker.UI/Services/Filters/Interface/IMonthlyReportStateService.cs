using MoneyTracker.Application.DTOs.Reports;

namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface IMonthlyReportStateService
    {
        Task SaveAsync(MonthlyReportStateDto state);
        Task<MonthlyReportStateDto?> GetAsync();
        Task ClearAsync();
    }
}
