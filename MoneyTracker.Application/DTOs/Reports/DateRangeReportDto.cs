namespace MoneyTracker.Application.DTOs.Reports
{
    public class DateRangeReportDto
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public string DateRangeLabel => $"{From:MMM d} – {To:MMM d, yyyy}";

        public ReportSummaryDto Summary { get; set; } = new();
        public List<ReportTransactionDto> Transactions { get; set; } = new();
        public List<ReportCategorySummaryDto> CategorySummary { get; set; } = new();
    }
}
