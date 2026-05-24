using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class TransactionRule : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [MaxLength(200)]
        public string? Name { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool ApplyToHistorical { get; set; } = true;
        public DateTime? ApplyFromDate { get; set; }

        public int Order { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public ICollection<TransactionRuleCondition> Conditions { get; set; } = new List<TransactionRuleCondition>();
        public ICollection<TransactionRuleAction> Actions { get; set; } = new List<TransactionRuleAction>();
    }
}
