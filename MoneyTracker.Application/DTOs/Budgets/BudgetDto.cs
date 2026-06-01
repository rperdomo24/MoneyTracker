using MoneyTracker.Domain.Enums.Budgets;

namespace MoneyTracker.Application.DTOs.Budgets
{
    public class BudgetDto
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
        public bool IncludeChildren { get; set; }
        public bool RolloverEnabled { get; set; }
        public RolloverMode RolloverMode { get; set; }
        public PaycheckPeriod PaycheckPeriod { get; set; } = PaycheckPeriod.Both;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
