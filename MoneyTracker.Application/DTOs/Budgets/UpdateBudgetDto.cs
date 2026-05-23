using MoneyTracker.Domain.Enums.Budgets;

namespace MoneyTracker.Application.DTOs.Budgets
{
    public class UpdateBudgetDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public bool IncludeChildren { get; set; }
        public bool RolloverEnabled { get; set; }
        public RolloverMode RolloverMode { get; set; }
    }
}
