using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTracker.Domain.Entities
{
    public class RecurringTransaction : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public int CategoryId { get; set; }
        public int AccountId { get; set; }

        public RecurringFrequency Frequency { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime NextDate { get; set; }
        public DateTime? LastGeneratedDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? TotalOccurrences { get; set; }
        public int OccurrencesGenerated { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Description { get; set; }

        public PaymentMethodEnum? PaymentMethod { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Category? Category { get; set; }
        public Account? Account { get; set; }
    }
}
