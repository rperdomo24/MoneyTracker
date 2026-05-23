using MoneyTracker.Application.DTOs.Budgets;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers
{
    public static class BudgetMapper
    {
        public static BudgetDto MapToDto(this Budget entity)
        {
            return new BudgetDto
            {
                Id = entity.Id,
                CategoryId = entity.CategoryId,
                Year = entity.Year,
                Month = entity.Month,
                Amount = entity.Amount,
                IncludeChildren = entity.IncludeChildren,
                RolloverEnabled = entity.RolloverEnabled,
                RolloverMode = entity.RolloverMode
            };
        }

        public static Budget MapToEntity(this CreateBudgetDto dto, MoneyTracker.Application.Interfaces.ITimeZoneService timeZoneService)
        {
            var nowUtc = timeZoneService.GetNowInUtc();
            return new Budget
            {
                CategoryId = dto.CategoryId,
                Year = dto.Year,
                Month = dto.Month,
                Amount = dto.Amount,
                IncludeChildren = dto.IncludeChildren,
                RolloverEnabled = dto.RolloverEnabled,
                RolloverMode = dto.RolloverMode,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };
        }

        public static void UpdateEntity(this Budget entity, UpdateBudgetDto dto, MoneyTracker.Application.Interfaces.ITimeZoneService timeZoneService)
        {
            entity.Amount = dto.Amount;
            entity.IncludeChildren = dto.IncludeChildren;
            entity.RolloverEnabled = dto.RolloverEnabled;
            entity.RolloverMode = dto.RolloverMode;
            entity.UpdatedAt = timeZoneService.GetNowInUtc();
        }
    }
}
