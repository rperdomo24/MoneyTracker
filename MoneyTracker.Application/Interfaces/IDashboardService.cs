using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Dashboard;
using MoneyTracker.Domain.Enums;

namespace MoneyTracker.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<OperationResult<DashboardSummaryDto>> GetSummaryAsync();
        Task<OperationResult<CashFlowDto>> GetCashFlowAsync(TimePeriodFilter period = TimePeriodFilter.ThisMonth);
        Task<OperationResult<List<FinancialAlertDto>>> GetAlertsAsync();
        Task<OperationResult<List<BalanceTrendDto>>> GetBalanceTrendAsync(int months = 6);
        Task<OperationResult<List<CategoryBreakdownDto>>> GetCategoryBreakdownAsync(TimePeriodFilter period = TimePeriodFilter.ThisMonth);
        Task<OperationResult<List<AccountActivityDto>>> GetAccountActivityAsync();
        Task<OperationResult<List<MonthlyComparisonDto>>> GetMonthlyComparisonAsync();
        Task<OperationResult<List<RecentTransactionDto>>> GetRecentTransactionsAsync(int count = 10);
    }
}
