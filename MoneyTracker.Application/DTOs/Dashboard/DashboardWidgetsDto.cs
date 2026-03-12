namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class DashboardWidgetsDto
    {
        public CashFlowDto CashFlow { get; set; } = new();
        public DashboardBudgetSummaryDto BudgetSummary { get; set; } = new();
        public List<DashboardSpendingTrendDto> SpendingTrend { get; set; } = new();
        public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    }
}
