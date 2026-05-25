using MoneyTracker.Domain.Enums.Loans;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class Loan : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; } = 0m;
        public int? NumberOfInstallments { get; set; }
        public DateTime? FirstPaymentDate { get; set; }
        public PaymentFrequency? PaymentFrequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public LoanStatus Status { get; set; } = LoanStatus.Active;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
        public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
    }
}
