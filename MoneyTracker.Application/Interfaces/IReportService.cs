using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Reports;

namespace MoneyTracker.Application.Interfaces
{
    public interface IReportService
    {
        Task<OperationResult<MonthlyReportDto>> GetMonthlyReportAsync(MonthlyReportFilterDto filter);
        Task<OperationResult<DateRangeReportDto>> GetRangeReportAsync(DateRangeReportFilterDto filter);
        Task<OperationResult<string>> GetAiAnalysisAsync(MonthlyReportDto report, bool forceRefresh = false);
        Task<OperationResult<string>> GetCachedAiAnalysisAsync(int year, int month);
        Task<OperationResult<string>> GetRangeAiAnalysisAsync(DateRangeReportDto report, bool forceRefresh = false);
        Task<OperationResult<string>> GetCachedRangeAiAnalysisAsync(DateOnly from, DateOnly to);
    }
}
