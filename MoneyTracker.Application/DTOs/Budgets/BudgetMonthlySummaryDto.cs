namespace MoneyTracker.Application.DTOs.Budgets
{
    public class BudgetMonthlySummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal TotalBudgeted { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal TotalRemaining => TotalBudgeted - TotalUsed;

        public List<BudgetMonthlyItemDto> Items { get; set; } = new();
    }
}
