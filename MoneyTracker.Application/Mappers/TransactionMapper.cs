using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers
{
    public static class TransactionMapper
    {
        public static TransactionDto MapToDto(Transaction entity)
        {
            return new TransactionDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Date = entity.Date,
                Amount = entity.Amount,
                Description = entity.Description,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.Name,
                AccountId = entity.AccountId,
                AccountName = entity.Account?.Name,
                PaymentMethod = entity.PaymentMethod,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public static Transaction MapToEntity(TransactionDto dto)
        {
            return new Transaction
            {
                Id = dto.Id,
                Name = dto.Name,
                Date = dto.Date,
                Amount = dto.Amount,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                AccountId = dto.AccountId,
                PaymentMethod = dto.PaymentMethod,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };
        }

        public static void UpdateEntity(Transaction entity, TransactionDto dto)
        {
            entity.Name = dto.Name;
            entity.Date = dto.Date;
            entity.Amount = dto.Amount;
            entity.Description = dto.Description;
            entity.CategoryId = dto.CategoryId;
            entity.AccountId = dto.AccountId;
            entity.PaymentMethod = dto.PaymentMethod;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
