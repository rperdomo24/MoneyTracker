namespace MoneyTracker.Application.DTOs.Loans
{
    public class LoanInstallmentDto
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal ExpectedAmount { get; set; }
        public string? Notes { get; set; }

        // Computed in mapper
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public bool IsPaid { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsPartiallyPaid { get; set; }
    }
}
