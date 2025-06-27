namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal NetWorth { get; set; }
        public decimal MonthlyChange { get; set; }
        public decimal MonthlyChangePercentage { get; set; }
        public bool IsPositiveChange { get; set; }
        public List<decimal> Last6MonthsNetWorth { get; set; } = new();
    }
}
