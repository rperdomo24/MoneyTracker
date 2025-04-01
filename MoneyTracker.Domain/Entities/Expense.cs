namespace MoneyTracker.Domain.Entities
{
    public class Expense
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = default!;
        public string Category { get; set; } = default!;

        public Guid AccountId { get; set; }
        public Account Account { get; set; } = default!;
    }
}
