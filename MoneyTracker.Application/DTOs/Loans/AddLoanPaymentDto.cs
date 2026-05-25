namespace MoneyTracker.Application.DTOs.Loans
{
    public class AddLoanPaymentDto
    {
        public int LoanId { get; set; }
        public int? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
