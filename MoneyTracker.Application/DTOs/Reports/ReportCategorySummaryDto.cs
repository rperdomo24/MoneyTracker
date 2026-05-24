namespace MoneyTracker.Application.DTOs.Reports
{
    public class ReportCategorySummaryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int TransactionCount { get; set; }
    }
}
