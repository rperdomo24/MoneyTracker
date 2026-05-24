using MoneyTracker.Domain.Enums.CardBenefits;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Application.DTOs.CardBenefits
{
    public class CardBenefitDto
    {
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        [MaxLength(200)]
        public string? MerchantPattern { get; set; }

        [Required]
        public BenefitType BenefitType { get; set; }

        [Required]
        [Range(0.0001, 1.0)]
        public decimal BenefitRate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxMonthlyCap { get; set; }

        public bool IsActive { get; set; } = true;

        // View model
        public string AccountName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string BenefitRateFormatted => $"{BenefitRate * 100:F1}%";
    }
}
