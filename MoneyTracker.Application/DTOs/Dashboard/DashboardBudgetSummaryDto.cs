namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class DashboardBudgetSummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalBudgeted { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal TotalRemaining { get; set; }
        public int ProgressPercent { get; set; }
        public int BudgetedCategoryCount { get; set; }
        public int NearLimitCount { get; set; }
        public int OverBudgetCount { get; set; }
        public int NoBudgetActivityCount { get; set; }
        public string HealthLabel { get; set; } = "No budgets";
        public List<DashboardBudgetItemDto> TopCategories { get; set; } = new();
    }

    public class DashboardBudgetItemDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = "#9e9e9e";
        public decimal BudgetAmount { get; set; }
        public decimal UsedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public int ProgressPercent { get; set; }
        public bool IsOverBudget { get; set; }
        public bool IsNearLimit { get; set; }
        public bool IsGroupOnly { get; set; }
    }
}
