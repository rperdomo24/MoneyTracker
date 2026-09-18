using MoneyTracker.Domain.Enums.CardBenefits;
using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTracker.Domain.Entities
{
    public class CardBenefit : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        public int AccountId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        [MaxLength(200)]
        public string? MerchantPattern { get; set; }

        public BenefitType BenefitType { get; set; }

        [Column(TypeName = "decimal(5,4)")]
        public decimal BenefitRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxMonthlyCap { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public Account? Account { get; set; }
        public Category? Category { get; set; }
    }
}
