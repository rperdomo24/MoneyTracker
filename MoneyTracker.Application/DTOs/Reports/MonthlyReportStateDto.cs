namespace MoneyTracker.Application.DTOs.Reports
{
    public class MonthlyReportStateDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public List<string> SelectedCategories { get; set; } = new();
        public string? ActiveCardFilter { get; set; }
    }
}
