namespace MoneyTracker.Application.DTOs.Loans
{
    public class UpdateLoanPaymentDto
    {
        public int Id { get; set; }
        public int? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
    }
}
