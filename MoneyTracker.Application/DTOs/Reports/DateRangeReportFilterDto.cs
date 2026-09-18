namespace MoneyTracker.Application.DTOs.Reports
{
    public class DateRangeReportFilterDto
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public List<string> CategoryNames { get; set; } = new();
    }
}
