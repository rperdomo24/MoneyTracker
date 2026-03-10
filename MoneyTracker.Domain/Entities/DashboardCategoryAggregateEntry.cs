namespace MoneyTracker.Domain.Entities
{
    public class DashboardCategoryAggregateEntry
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = "#9e9e9e";
        public decimal Amount { get; set; }
        public int TransactionCount { get; set; }
    }
}
