using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers;

public static class TransactionMapper
{
    public static TransactionDto MapToDto(this Transaction entity, ITimeZoneService timeZoneService)
    {
        return new TransactionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Date = timeZoneService.ConvertFromUtc(entity.Date),
            Amount = entity.Amount,
            Description = entity.Description,
            CategoryId = entity.CategoryId,
            Category = entity.Category?.MapToDto(),
            AccountId = entity.AccountId,
            Account = entity.Account?.MapToDto(),
            CreatedAt = timeZoneService.ConvertFromUtc(entity.CreatedAt),
            UpdatedAt = timeZoneService.ConvertFromUtc(entity.UpdatedAt)
        };
    }

    public static Transaction MapToEntity(this TransactionDto dto, ITimeZoneService timeZoneService)
    {
        return new Transaction
        {
            Id = dto.Id,
            Name = dto.Name,
            Date = timeZoneService.ConvertToUtc(dto.Date),
            Amount = dto.Amount,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            AccountId = dto.AccountId,
            CreatedAt = timeZoneService.ConvertToUtc(dto.CreatedAt),
            UpdatedAt = timeZoneService.ConvertToUtc(dto.UpdatedAt)
        };
    }

    public static void UpdateEntity(this Transaction entity, TransactionDto dto, ITimeZoneService timeZoneService)
    {
        entity.Name = dto.Name;
        entity.Date = timeZoneService.ConvertToUtc(dto.Date);
        entity.Amount = dto.Amount;
        entity.Description = dto.Description;
        entity.CategoryId = dto.CategoryId;
        entity.AccountId = dto.AccountId;
        entity.UpdatedAt = timeZoneService.GetNowInUtc();
    }
}
