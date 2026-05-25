namespace MoneyTracker.Application.DTOs.Goals
{
    public class SavingsContributionDto
    {
        public int Id { get; set; }
        public int GoalId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public int? LinkedTransactionId { get; set; }
        public string? LinkedTransactionName { get; set; }
        public string? LinkedTransactionAccount { get; set; }
        public DateTime? LinkedTransactionDate { get; set; }
        public decimal? LinkedTransactionAmount { get; set; }
    }
}
