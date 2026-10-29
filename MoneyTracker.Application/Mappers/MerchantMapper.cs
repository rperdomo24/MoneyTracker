using MoneyTracker.Application.DTOs.Merchants;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers
{
    public static class MerchantMapper
    {
        public static MerchantDto MapToDto(this Merchant entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            IsHidden = entity.IsHidden
        };

        public static Merchant MapToEntity(this MerchantDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name.Trim(),
            IsHidden = dto.IsHidden
        };

        public static void UpdateEntity(this Merchant entity, MerchantDto dto)
        {
            entity.Name = dto.Name.Trim();
            entity.IsHidden = dto.IsHidden;
        }
    }
}
