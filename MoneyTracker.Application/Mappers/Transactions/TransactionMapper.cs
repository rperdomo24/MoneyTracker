using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers.Transactions;

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
            Type = entity.Type,
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
            Type = dto.Type,
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
        entity.Type = dto.Type;
        entity.UpdatedAt = timeZoneService.GetNowInUtc();
    }
}
