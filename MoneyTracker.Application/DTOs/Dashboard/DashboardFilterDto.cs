using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class DashboardFilterDto
    {
        public TimePeriodFilter TimePeriod { get; set; } = TimePeriodFilter.Last30Days;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<int> AccountIds { get; set; } = new();
        public DashboardTransactionFilter TransactionFilter { get; set; } = DashboardTransactionFilter.All;
    }

    public enum DashboardTransactionFilter
    {
        All = 0,
        Income = 1,
        Expense = 2
    }
}
