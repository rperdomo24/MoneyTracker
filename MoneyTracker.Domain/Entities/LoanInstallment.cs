namespace MoneyTracker.Domain.Entities
{
    public class LoanInstallment
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal ExpectedAmount { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
