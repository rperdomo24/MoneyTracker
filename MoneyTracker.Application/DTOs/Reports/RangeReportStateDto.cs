namespace MoneyTracker.Application.DTOs.Reports
{
    public class RangeReportStateDto
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public List<string> PreFilterCategories { get; set; } = new();
        public List<string> TableFilter { get; set; } = new();
    }
}
