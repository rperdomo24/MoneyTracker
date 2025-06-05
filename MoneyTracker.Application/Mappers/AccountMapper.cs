using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;

namespace MoneyTracker.Application.Mappers
{
    public static class AccountMapper
    {
        public static AccountDto MapToDto(this Account entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Balance = entity.Balance,
            CreditLimit = entity.CreditLimit,
            Notes = entity.Notes,
            Icon = Enum.TryParse<AccountIcon>(entity.Icon, out var parsedIcon) ? parsedIcon : AccountIcon.Wallet,
            Color = entity.Color,
            Type = entity.Type,
        };

        public static Account MapToEntity(this AccountDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Balance = dto.Balance,
            CreditLimit = dto.CreditLimit,
            Notes = dto.Notes,
            Icon = dto.Icon.ToString(),
            Color = dto.Color,
            Type = dto.Type
        };
    }
}
