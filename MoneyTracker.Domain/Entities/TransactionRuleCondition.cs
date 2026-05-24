using MoneyTracker.Domain.Enums.Rules;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class TransactionRuleCondition
    {
        public int Id { get; set; }
        public int RuleId { get; set; }

        public RuleConditionField Field { get; set; }
        public RuleConditionOperator Operator { get; set; }

        [Required]
        [MaxLength(500)]
        public string Value { get; set; } = string.Empty;

        public TransactionRule? Rule { get; set; }
    }
}
