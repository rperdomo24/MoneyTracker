using MoneyTracker.Domain.Enums.Budgets;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class Budget : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = default!;

        public int Year { get; set; }
        public int Month { get; set; }

        public decimal Amount { get; set; }

        public bool IncludeChildren { get; set; } = true;

        public bool RolloverEnabled { get; set; } = false;
        public RolloverMode RolloverMode { get; set; } = RolloverMode.None;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
