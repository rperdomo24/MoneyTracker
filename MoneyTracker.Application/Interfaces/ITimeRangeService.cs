using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.Application.Interfaces
{
    public interface ITimeRangeService
    {
        (DateTime? startDateUtc, DateTime? endDateUtc) GetDateRangeUtc(TimePeriodFilter timePeriod);
        (DateTime? startDateUtc, DateTime? endDateUtc) GetDateRangeUtc(TimePeriodFilter timePeriod, DateTime? fromDate, DateTime? toDate);
    }
}
