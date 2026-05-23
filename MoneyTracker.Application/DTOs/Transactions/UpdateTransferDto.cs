namespace MoneyTracker.Application.DTOs.Transactions
{
    public class UpdateTransferDto
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Description { get; set; }
    }
}
