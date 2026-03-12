namespace MoneyTracker.Application.DTOs.Transactions
{
    public class TransactionSummaryDto
    {
        public List<TransactionDto> Transactions { get; set; } = new();
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Balance { get; set; }
        public int TotalCount { get; set; }
    }
}
