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
            AccountId = entity.AccountId,
            CategoryId = entity.CategoryId,
            PaymentMethod = entity.PaymentMethod,
            Status = entity.Status,
            ScheduledDate = entity.ScheduledDate,
            IsSystemGenerated = entity.IsSystemGenerated,
            TransferPairId = entity.TransferPairId,
            TransferType = entity.TransferType,
            CreatedAt = timeZoneService.ConvertFromUtc(entity.CreatedAt),
            UpdatedAt = timeZoneService.ConvertFromUtc(entity.UpdatedAt),
            Account = entity.Account?.MapToDto(),
            Category = entity.Category?.MapToDto(),

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
            AccountId = dto.AccountId,
            CategoryId = dto.CategoryId,
            PaymentMethod = dto.PaymentMethod,
            Status = dto.Status,
            ScheduledDate = dto.ScheduledDate,
            IsSystemGenerated = dto.IsSystemGenerated,
            TransferPairId = dto.TransferPairId,
            TransferType = dto.TransferType,
            UpdatedAt = timeZoneService.ConvertToUtc(dto.UpdatedAt),
            CreatedAt = timeZoneService.ConvertToUtc(dto.CreatedAt),
            IsDeleted = false
        };
    }

    public static void UpdateEntity(this Transaction entity, TransactionDto dto, ITimeZoneService timeZoneService)
    {
        entity.Name = dto.Name;
        entity.Date = timeZoneService.ConvertToUtc(dto.Date);
        entity.Amount = dto.Amount;
        entity.Description = dto.Description;
        entity.AccountId = dto.AccountId;
        entity.CategoryId = dto.CategoryId;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.Status = dto.Status;
        entity.ScheduledDate = dto.ScheduledDate;
        entity.IsSystemGenerated = dto.IsSystemGenerated;
        entity.TransferPairId = dto.TransferPairId;
        entity.TransferType = dto.TransferType;
        entity.UpdatedAt = timeZoneService.GetNowInUtc();
    }
}
