using MoneyTracker.Application.Common.Extensions;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Transaction;

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
            MerchantId = entity.MerchantId,
            MerchantName = entity.Merchant?.Name,
            PaymentMethod = entity.PaymentMethod,
            Status = entity.Status,
            ScheduledDate = entity.ScheduledDate,
            IsSystemGenerated = entity.IsSystemGenerated,
            TransferPairId = entity.TransferPairId,
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
            Amount = dto.GetSignedAmount(),
            Description = dto.Description,
            AccountId = dto.AccountId,
            CategoryId = dto.CategoryId,
            MerchantId = dto.MerchantId,
            PaymentMethod = dto.PaymentMethod,
            Status = dto.Status,
            ScheduledDate = dto.ScheduledDate,
            IsSystemGenerated = dto.IsSystemGenerated,
            TransferPairId = dto.TransferPairId,
            UpdatedAt = timeZoneService.ConvertToUtc(dto.UpdatedAt),
            CreatedAt = timeZoneService.ConvertToUtc(dto.CreatedAt),
            IsDeleted = false
        };
    }

    public static void UpdateEntity(this Transaction entity, TransactionDto dto, ITimeZoneService timeZoneService)
    {
        entity.Name = dto.Name;
        entity.Date = timeZoneService.ConvertToUtc(dto.Date);
        entity.Amount = dto.GetSignedAmount();
        entity.Description = dto.Description;
        entity.AccountId = dto.AccountId;
        entity.CategoryId = dto.CategoryId;
        entity.MerchantId = dto.MerchantId;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.Status = dto.Status;
        entity.ScheduledDate = dto.ScheduledDate;
        entity.IsSystemGenerated = dto.IsSystemGenerated;
        entity.TransferPairId = dto.TransferPairId;
        entity.UpdatedAt = timeZoneService.GetNowInUtc();
    }

    public static Transaction MapToDuplicate(
    this Transaction original,
    ITimeZoneService timeZoneService)
    {
        var nowUtc = timeZoneService.GetNowInUtc();
        var todayUtc = timeZoneService.ConvertToUtc(DateTime.Today);

        return new Transaction
        {
            Name = $"{original.Name} (Copy)",
            Amount = original.Amount,
            Date = todayUtc,
            Description = original.Description,
            CategoryId = original.CategoryId,
            AccountId = original.AccountId,
            MerchantId = original.MerchantId,
            PaymentMethod = original.PaymentMethod,
            Status = TransactionStatus.Completed,
            IsSystemGenerated = false,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }
}
