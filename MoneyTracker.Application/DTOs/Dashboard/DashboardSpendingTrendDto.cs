namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class DashboardSpendingTrendDto
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
