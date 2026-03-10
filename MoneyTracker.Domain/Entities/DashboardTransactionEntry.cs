using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Domain.Entities
{
    public class DashboardTransactionEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = "#9e9e9e";
        public CategoryTypeEnum CategoryType { get; set; } = CategoryTypeEnum.Expense;
        public string? SystemCategoryCode { get; set; }
    }
}
