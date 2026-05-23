using MoneyTracker.Application.DTOs.Dashboard;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Application.Mappers
{
    public static class BalanceTrendMapper
    {
        public static BalanceTrendMovementDto MapToMovementDto(
            this TransactionTrendEntry entry,
            ITimeZoneService timeZoneService)
        {
            var localDate = timeZoneService.ConvertFromUtc(entry.Date).Date;
            var delta = entry.CategoryType switch
            {
                CategoryTypeEnum.Income => Math.Abs(entry.Amount),
                CategoryTypeEnum.Expense => -Math.Abs(entry.Amount),
                _ => 0m
            };

            return new BalanceTrendMovementDto
            {
                BucketDate = localDate,
                Delta = delta
            };
        }
    }
}
