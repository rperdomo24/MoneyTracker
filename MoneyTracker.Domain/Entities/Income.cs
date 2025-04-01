namespace MoneyTracker.Domain.Entities
{
    public class Income
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Source { get; set; } = default!;
        public string? Notes { get; set; }

        public Guid AccountId { get; set; }
        public Account Account { get; set; } = default!;
    }
}
