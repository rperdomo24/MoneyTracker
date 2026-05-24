namespace MoneyTracker.Application.DTOs.Reports
{
    public class ReportMerchantSummaryDto
    {
        public string MerchantName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int TransactionCount { get; set; }
        public string? TopCategory { get; set; }
    }
}
