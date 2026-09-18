namespace MoneyTracker.Application.DTOs.Rules
{
    public class TransactionRuleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool ApplyToHistorical { get; set; } = true;
        public DateTime? ApplyFromDate { get; set; }
        public int Order { get; set; } = 0;

        public List<TransactionRuleConditionDto> Conditions { get; set; } = new();
        public List<TransactionRuleActionDto> Actions { get; set; } = new();
    }
}
