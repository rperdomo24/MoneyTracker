using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Domain.Entities
{
    public class DashboardCashFlowAggregateEntry
    {
        public CategoryTypeEnum CategoryType { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int TransactionCount { get; set; }
    }
}
