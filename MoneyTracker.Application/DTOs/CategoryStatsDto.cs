namespace MoneyTracker.Application.DTOs
{
    public class CategoryStatsDto
    {
        public int TotalCount { get; set; }
        public decimal ThisMonthAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }
}
