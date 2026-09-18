using MoneyTracker.Domain.Enums.Budgets;

namespace MoneyTracker.Application.DTOs.Budgets
{
    public class CreateBudgetDto
    {
        public int CategoryId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
        public bool IncludeChildren { get; set; } = true;
        public bool RolloverEnabled { get; set; } = false;
        public RolloverMode RolloverMode { get; set; } = RolloverMode.None;
        public PaycheckPeriod PaycheckPeriod { get; set; } = PaycheckPeriod.Both;
    }
}
