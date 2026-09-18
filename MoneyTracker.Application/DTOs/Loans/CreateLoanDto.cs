using MoneyTracker.Domain.Enums.Loans;

namespace MoneyTracker.Application.DTOs.Loans
{
    public class CreateLoanDto
    {
        public string ContactName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; } = 0m;
        public int? NumberOfInstallments { get; set; }
        public DateTime? FirstPaymentDate { get; set; }
        public PaymentFrequency? PaymentFrequency { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
    }
}
