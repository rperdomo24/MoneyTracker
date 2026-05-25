using MoneyTracker.Domain.Enums.Loans;

namespace MoneyTracker.Application.DTOs.Loans
{
    public class LoanDto
    {
        public int Id { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public int? NumberOfInstallments { get; set; }
        public DateTime? FirstPaymentDate { get; set; }
        public PaymentFrequency? PaymentFrequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public LoanStatus Status { get; set; }
        public string? Notes { get; set; }

        // Computed
        public decimal InterestAmount { get; set; }
        public decimal TotalOwed { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public decimal OverpaymentAmount { get; set; }
        public bool IsOverdue { get; set; }
        public decimal ProgressPercent { get; set; }
        public decimal InstallmentQuota { get; set; }

        public List<LoanPaymentDto> Payments { get; set; } = new();
        public List<LoanInstallmentDto> Installments { get; set; } = new();
    }
}
