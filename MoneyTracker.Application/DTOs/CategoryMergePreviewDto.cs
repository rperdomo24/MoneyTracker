namespace MoneyTracker.Application.DTOs
{
    public class CategoryMergePreviewDto
    {
        public List<BudgetMergePreviewItemDto> Budgets { get; set; } = new();
        public List<string> SubcategoryNames { get; set; } = new();
    }

    public class BudgetMergePreviewItemDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
        public bool WillBeDropped { get; set; }
    }
}
