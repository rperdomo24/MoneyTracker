using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Domain.Entities
{
    public class TransactionTrendEntry
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public CategoryTypeEnum CategoryType { get; set; }
    }
}
