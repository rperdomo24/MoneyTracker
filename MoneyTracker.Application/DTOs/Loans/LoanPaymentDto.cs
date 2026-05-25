namespace MoneyTracker.Application.DTOs.Loans
{
    public class LoanPaymentDto
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public int? TransactionId { get; set; }
        public string? TransactionName { get; set; }
        public string? TransactionDescription { get; set; }
        public DateTime? TransactionDate { get; set; }
        public decimal? TransactionAmount { get; set; }
        public string? TransactionAccount { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
