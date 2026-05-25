using MoneyTracker.Application.DTOs.RecurringTransactions;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.Mappers
{
    public static class RecurringTransactionMapper
    {
        public static RecurringTransactionDto MapToDto(this RecurringTransaction entity)
        {
            return new RecurringTransactionDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Amount = entity.Amount,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.Name,
                AccountId = entity.AccountId,
                AccountName = entity.Account?.Name,
                Frequency = entity.Frequency,
                StartDate = entity.StartDate,
                NextDate = entity.NextDate,
                LastGeneratedDate = entity.LastGeneratedDate,
                IsActive = entity.IsActive,
                Description = entity.Description,
                PaymentMethod = entity.PaymentMethod
            };
        }

        public static RecurringTransaction MapToEntity(this RecurringTransactionDto dto)
        {
            return new RecurringTransaction
            {
                Id = dto.Id,
                Name = dto.Name,
                Amount = dto.Amount,
                CategoryId = dto.CategoryId,
                AccountId = dto.AccountId,
                Frequency = dto.Frequency,
                StartDate = dto.StartDate.ToUniversalTime(),
                NextDate = dto.StartDate.ToUniversalTime(),
                IsActive = dto.IsActive,
                Description = dto.Description,
                PaymentMethod = dto.PaymentMethod
            };
        }

        public static void UpdateEntity(this RecurringTransaction entity, RecurringTransactionDto dto)
        {
            entity.Name = dto.Name;
            entity.Amount = dto.Amount;
            entity.CategoryId = dto.CategoryId;
            entity.AccountId = dto.AccountId;
            entity.Frequency = dto.Frequency;
            entity.IsActive = dto.IsActive;
            entity.Description = dto.Description;
            entity.PaymentMethod = dto.PaymentMethod;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        public static DateTime AdvanceNextDate(DateTime current, RecurringFrequency frequency)
        {
            return frequency switch
            {
                RecurringFrequency.Daily => current.AddDays(1),
                RecurringFrequency.Weekly => current.AddDays(7),
                RecurringFrequency.BiWeekly => current.AddDays(14),
                RecurringFrequency.Monthly => current.AddMonths(1),
                RecurringFrequency.BiMonthly => current.AddMonths(2),
                RecurringFrequency.Quarterly => current.AddMonths(3),
                RecurringFrequency.Yearly => current.AddYears(1),
                _ => current.AddMonths(1)
            };
        }
    }
}
