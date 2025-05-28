using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers
{
    public static class AccountMapper
    {
        public static AccountDto MapToDto(this Account entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Balance = entity.Balance,
            CreditLimit = entity.CreditLimit,
            Color = entity.Color,
            Notes = entity.Notes
        };

        public static Account MapToEntity(this AccountDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Type = dto.Type,
            Balance = dto.Balance,
            CreditLimit = dto.CreditLimit,
            Color = dto.Color,
            Notes = dto.Notes
        };
    }
}
