using MoneyTracker.Domain.Enums.Rules;

namespace MoneyTracker.Application.DTOs.Rules
{
    public class TransactionRuleConditionDto
    {
        public int Id { get; set; }
        public int RuleId { get; set; }
        public RuleConditionField Field { get; set; }
        public RuleConditionOperator Operator { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
