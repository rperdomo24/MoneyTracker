using MoneyTracker.Application.DTOs.CardBenefits;

namespace MoneyTracker.Application.DTOs.Reports
{
    public class MonthlyReportDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

        public ReportSummaryDto Summary { get; set; } = new();
        public List<ReportTransactionDto> Transactions { get; set; } = new();
        public List<ReportCategorySummaryDto> CategorySummary { get; set; } = new();
        public List<ReportCardSummaryDto> CardSummary { get; set; } = new();
        public List<CardBenefitDto> BenefitRules { get; set; } = new();
        public List<ReportRecommendationDto> Recommendations { get; set; } = new();
        public List<ReportMerchantSummaryDto> MerchantSummary { get; set; } = new();
        public string? AiAnalysis { get; set; }
    }
}
