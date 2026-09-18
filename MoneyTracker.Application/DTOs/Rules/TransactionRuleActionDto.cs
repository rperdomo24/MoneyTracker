using MoneyTracker.Domain.Enums.Rules;

namespace MoneyTracker.Application.DTOs.Rules
{
    public class TransactionRuleActionDto
    {
        public int Id { get; set; }
        public int RuleId { get; set; }
        public RuleActionType ActionType { get; set; }
        public int? MerchantId { get; set; }
        public int? CategoryId { get; set; }
        public string? StringValue { get; set; }

        // Denormalized for display
        public string? MerchantName { get; set; }
        public string? CategoryName { get; set; }
    }
}
