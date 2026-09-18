namespace MoneyTracker.Domain.Entities
{
    public class SavingsContribution
    {
        public int Id { get; set; }
        public int GoalId { get; set; }
        public SavingsGoal Goal { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? LinkedTransactionId { get; set; }
        public Transaction? LinkedTransaction { get; set; }
    }
}
