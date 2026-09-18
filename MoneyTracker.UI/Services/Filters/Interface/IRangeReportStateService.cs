using MoneyTracker.Application.DTOs.Reports;

namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface IRangeReportStateService
    {
        Task SaveAsync(RangeReportStateDto state);
        Task<RangeReportStateDto?> GetAsync();
        Task ClearAsync();
    }
}
