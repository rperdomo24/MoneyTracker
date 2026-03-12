using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTracker.Domain.Entities
{
    public class Transaction : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public int AccountId { get; set; }
        public int CategoryId { get; set; }
        public PaymentMethodEnum? PaymentMethod { get; set; } = PaymentMethodEnum.Cash;

        // For scheduled transactions
        public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
        public DateTime? ScheduledDate { get; set; }
        public bool IsSystemGenerated { get; set; } = false;

        // For transfers
        public int? TransferPairId { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public Category? Category { get; set; } = null!;
        public Account? Account { get; set; } = null!;
        public virtual ICollection<TransactionAttachment> Attachments { get; set; } = new List<TransactionAttachment>();
    }
}
