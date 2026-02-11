namespace MoneyTracker.Application.DTOs.Budgets
{
    public class BudgetMonthlyItemDto
    {
        public BudgetDto Budget { get; set; } = new();
        public CategoryDto Category { get; set; } = new();
        public decimal Used { get; set; }
        public decimal Remaining => Budget.Amount - Used;
        public decimal PercentUsed
            => Budget.Amount <= 0 ? 0 : Math.Min(100, Math.Abs(Used) / Budget.Amount * 100);
    }
}
