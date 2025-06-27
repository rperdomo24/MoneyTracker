namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class MonthlyComparisonDto
    {
        public string Metric { get; set; } = string.Empty;
        public decimal CurrentMonth { get; set; }
        public decimal PreviousMonth { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercentage { get; set; }
        public bool IsImprovement { get; set; }
        public string Icon { get; set; } = string.Empty;
    }

}
