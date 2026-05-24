namespace MoneyTracker.Application.DTOs.Reports
{
    public class ReportCardSummaryDto
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public decimal TotalSpent { get; set; }
        public int TransactionCount { get; set; }
        public decimal EstimatedBenefit { get; set; }
    }
}
