namespace MoneyTracker.Application.DTOs.Budgets
{
    public class BudgetCategorySliceVm
    {
        public int CategoryId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#757575";
        public decimal Used { get; set; }
        public int Percent { get; set; }
        public List<int> CategoryIds { get; set; } = new();
        public bool IsWithoutBudget { get; set; }
    }
}
