namespace MoneyTracker.Application.DTOs.Reports
{
    public enum RecommendationType
    {
        WrongCardUsed = 1,
        MissedBenefit = 2,
        TopSpendingCategory = 3,
        HighCashUsage = 4
    }

    public class ReportRecommendationDto
    {
        public RecommendationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal? EstimatedLoss { get; set; }
        public string? SuggestedCard { get; set; }
    }
}
