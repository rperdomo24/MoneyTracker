using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Enums;

namespace MoneyTracker.Application.Mappers.Transactions
{
    public static class TransactionFilterMapper
    {
        public static (TimePeriodFilter timePeriod, DateTime? fromDateUtc, DateTime? toDateUtc, List<int> accountIds, List<int> transactionTypeIds)
            MapToRepositoryParameters(this TransactionFilterDto dto, ITimeZoneService timeZoneService)
        {
            DateTime? fromDateUtc = null;
            DateTime? toDateUtc = null;

            // Convertir fechas a UTC solo si están definidas
            if (dto.FromDate.HasValue)
            {
                fromDateUtc = timeZoneService.ConvertToUtc(dto.FromDate.Value);
            }

            if (dto.ToDate.HasValue)
            {
                // Para ToDate, incluir todo el día hasta 23:59:59
                var endOfDay = dto.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                toDateUtc = timeZoneService.ConvertToUtc(endOfDay);
            }

            return (
                timePeriod: dto.TimePeriod,
                fromDateUtc: fromDateUtc,
                toDateUtc: toDateUtc,
                accountIds: dto.AccountIds,
                transactionTypeIds: dto.TransactionTypeIds
            );
        }

        public static TransactionFilterDto MapFromRepositoryParameters(
            TimePeriodFilter timePeriod,
            DateTime? fromDateUtc,
            DateTime? toDateUtc,
            List<int> accountIds,
            List<int> transactionTypeIds,
            ITimeZoneService timeZoneService)
        {
            return new TransactionFilterDto
            {
                TimePeriod = timePeriod,
                FromDate = fromDateUtc.HasValue ? timeZoneService.ConvertFromUtc(fromDateUtc.Value) : null,
                ToDate = toDateUtc.HasValue ? timeZoneService.ConvertFromUtc(toDateUtc.Value) : null,
                AccountIds = accountIds,
                TransactionTypeIds = transactionTypeIds
            };
        }

        public static TransactionFilterDto ToUtcFilter(this TransactionFilterDto dto, ITimeZoneService timeZoneService)
        {
            var (timePeriod, fromDateUtc, toDateUtc, accountIds, transactionTypeIds) = dto.MapToRepositoryParameters(timeZoneService);

            return new TransactionFilterDto
            {
                TimePeriod = timePeriod,
                FromDate = fromDateUtc,
                ToDate = toDateUtc,
                AccountIds = accountIds,
                TransactionTypeIds = transactionTypeIds,
                SearchText = dto.SearchText,
                CategoryId = dto.CategoryId,
                Type = dto.Type
            };
        }
    }
}
