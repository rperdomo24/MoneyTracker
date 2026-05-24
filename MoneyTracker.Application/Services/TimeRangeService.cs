using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.Application.Services
{
    public class TimeRangeService : ITimeRangeService
    {
        private readonly ITimeZoneService _timeZoneService;

        public TimeRangeService(ITimeZoneService timeZoneService)
        {
            _timeZoneService = timeZoneService;
        }

        public (DateTime? startDateUtc, DateTime? endDateUtc) GetDateRangeUtc(TimePeriodFilter timePeriod)
        {
            return GetDateRangeUtc(timePeriod, null, null);
        }

        public (DateTime? startDateUtc, DateTime? endDateUtc) GetDateRangeUtc(TimePeriodFilter timePeriod, DateTime? fromDate, DateTime? toDate)
        {
            if (timePeriod == TimePeriodFilter.Custom)
            {
                return GetCustomRangeUtc(fromDate, toDate);
            }

            var userLocalNow = _timeZoneService.GetLocalTimeInConfiguredTimeZone();
            var userToday = userLocalNow.Date;

            return timePeriod switch
            {
                TimePeriodFilter.Last7Days => (
                    _timeZoneService.ConvertToUtc(userToday.AddDays(-7)),
                    _timeZoneService.ConvertToUtc(userToday.AddDays(1).AddTicks(-1))
                ),

                TimePeriodFilter.Last30Days => (
                    _timeZoneService.ConvertToUtc(userToday.AddDays(-30)),
                    _timeZoneService.ConvertToUtc(userToday.AddDays(1).AddTicks(-1))
                ),

                TimePeriodFilter.Last90Days => (
                    _timeZoneService.ConvertToUtc(userToday.AddDays(-90)),
                    _timeZoneService.ConvertToUtc(userToday.AddDays(1).AddTicks(-1))
                ),

                TimePeriodFilter.LastMonth => GetMonthRangeUtc(userToday.AddMonths(-1)),
                TimePeriodFilter.ThisMonth => GetMonthRangeUtc(userToday),
                TimePeriodFilter.ThisYear => GetYearRangeUtc(userToday.Year),

                TimePeriodFilter.Q1ThisMonth => (
                    _timeZoneService.ConvertToUtc(new DateTime(userToday.Year, userToday.Month, 1)),
                    _timeZoneService.ConvertToUtc(new DateTime(userToday.Year, userToday.Month, 15, 23, 59, 59))
                ),

                TimePeriodFilter.Q2ThisMonth => (
                    _timeZoneService.ConvertToUtc(new DateTime(userToday.Year, userToday.Month, 16)),
                    _timeZoneService.ConvertToUtc(new DateTime(userToday.Year, userToday.Month,
                        DateTime.DaysInMonth(userToday.Year, userToday.Month), 23, 59, 59))
                ),

                TimePeriodFilter.AllTime => (null, null),
                _ => (null, null)
            };
        }

        private (DateTime startDateUtc, DateTime endDateUtc) GetMonthRangeUtc(DateTime monthDate)
        {
            var start = new DateTime(monthDate.Year, monthDate.Month, 1);
            var end = new DateTime(monthDate.Year, monthDate.Month,
                DateTime.DaysInMonth(monthDate.Year, monthDate.Month), 23, 59, 59);

            return (_timeZoneService.ConvertToUtc(start), _timeZoneService.ConvertToUtc(end));
        }

        private (DateTime startDateUtc, DateTime endDateUtc) GetYearRangeUtc(int year)
        {
            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31, 23, 59, 59);

            return (_timeZoneService.ConvertToUtc(start), _timeZoneService.ConvertToUtc(end));
        }

        private (DateTime? startDateUtc, DateTime? endDateUtc) GetCustomRangeUtc(DateTime? fromDate, DateTime? toDate)
        {
            DateTime? fromDateUtc = null;
            DateTime? toDateUtc = null;

            if (fromDate.HasValue)
                fromDateUtc = _timeZoneService.ConvertToUtc(fromDate.Value);

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                toDateUtc = _timeZoneService.ConvertToUtc(endOfDay);
            }

            return (fromDateUtc, toDateUtc);
        }
    }
}