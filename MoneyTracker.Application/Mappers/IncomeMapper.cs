using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers
{
    public static class IncomeMapper
    {
        public static IncomeDto MapToDto(this Income entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Date = entity.Date,
            Amount = entity.Amount,
            Description = entity.Description,
            CategoryId = entity.CategoryId,
            AccountId = entity.AccountId,
            PaymentMethod = entity.PaymentMethod
        };

        public static Income MapToEntity(this IncomeDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Date = dto.Date,
            Amount = dto.Amount,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            AccountId = dto.AccountId,
            PaymentMethod = dto.PaymentMethod,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
