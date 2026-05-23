using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Dashboard;
using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<OperationResult<DashboardOverviewDto>> GetOverviewAsync(
            DashboardFilterDto? filter = null,
            int recentTransactionsCount = 10,
            int balanceTrendMonths = 6);
        Task<OperationResult<DashboardWidgetsDto>> GetOverviewWidgetsAsync(
            DashboardFilterDto? filter = null,
            int recentTransactionsCount = 10);
        Task<OperationResult<DashboardBudgetSummaryDto>> GetBudgetSummaryAsync(DashboardFilterDto? filter = null);
        Task<OperationResult<List<DashboardSpendingTrendDto>>> GetSpendingTrendAsync(DashboardFilterDto filter);
        Task<OperationResult<CashFlowDto>> GetCashFlowAsync(DashboardFilterDto filter);
        Task<OperationResult<List<CategoryBreakdownDto>>> GetCategoryBreakdownAsync(DashboardFilterDto filter);
        Task<OperationResult<List<RecentTransactionDto>>> GetRecentTransactionsAsync(
            DashboardFilterDto filter,
            int count = 10);
        Task<OperationResult<DashboardSummaryDto>> GetSummaryAsync();
        Task<OperationResult<CashFlowDto>> GetCashFlowAsync(TimePeriodFilter period = TimePeriodFilter.ThisMonth);
        Task<OperationResult<List<FinancialAlertDto>>> GetAlertsAsync();
        Task<OperationResult<List<BalanceTrendDto>>> GetBalanceTrendAsync(int months = 6);
        Task<OperationResult<List<BalanceTrendDto>>> GetBalanceTrendAsync(DashboardFilterDto filter);
        Task<OperationResult<List<CategoryBreakdownDto>>> GetCategoryBreakdownAsync(TimePeriodFilter period = TimePeriodFilter.ThisMonth);
        Task<OperationResult<List<AccountActivityDto>>> GetAccountActivityAsync();
        Task<OperationResult<List<MonthlyComparisonDto>>> GetMonthlyComparisonAsync();
        Task<OperationResult<List<RecentTransactionDto>>> GetRecentTransactionsAsync(int count = 10);
    }
}
