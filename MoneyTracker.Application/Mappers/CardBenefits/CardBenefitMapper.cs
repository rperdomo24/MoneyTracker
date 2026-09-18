using MoneyTracker.Application.DTOs.CardBenefits;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers.CardBenefits
{
    public static class CardBenefitMapper
    {
        public static CardBenefitDto MapToDto(this CardBenefit entity)
        {
            return new CardBenefitDto
            {
                Id = entity.Id,
                AccountId = entity.AccountId,
                Name = entity.Name,
                CategoryId = entity.CategoryId,
                MerchantPattern = entity.MerchantPattern,
                BenefitType = entity.BenefitType,
                BenefitRate = entity.BenefitRate,
                MaxMonthlyCap = entity.MaxMonthlyCap,
                IsActive = entity.IsActive,
                AccountName = entity.Account?.Name ?? string.Empty,
                CategoryName = entity.Category?.Name
            };
        }

        public static CardBenefit MapToEntity(this CardBenefitDto dto)
        {
            return new CardBenefit
            {
                Id = dto.Id,
                AccountId = dto.AccountId,
                Name = dto.Name,
                CategoryId = dto.CategoryId,
                MerchantPattern = dto.MerchantPattern,
                BenefitType = dto.BenefitType,
                BenefitRate = dto.BenefitRate,
                MaxMonthlyCap = dto.MaxMonthlyCap,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static void UpdateEntity(this CardBenefit entity, CardBenefitDto dto)
        {
            entity.Name = dto.Name;
            entity.AccountId = dto.AccountId;
            entity.CategoryId = dto.CategoryId;
            entity.MerchantPattern = dto.MerchantPattern;
            entity.BenefitType = dto.BenefitType;
            entity.BenefitRate = dto.BenefitRate;
            entity.MaxMonthlyCap = dto.MaxMonthlyCap;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
