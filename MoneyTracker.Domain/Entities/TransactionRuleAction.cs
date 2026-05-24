using MoneyTracker.Domain.Enums.Rules;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class TransactionRuleAction
    {
        public int Id { get; set; }
        public int RuleId { get; set; }

        public RuleActionType ActionType { get; set; }

        public int? MerchantId { get; set; }
        public int? CategoryId { get; set; }

        [MaxLength(100)]
        public string? StringValue { get; set; }

        public TransactionRule? Rule { get; set; }
        public Merchant? Merchant { get; set; }
        public Category? Category { get; set; }
    }
}
