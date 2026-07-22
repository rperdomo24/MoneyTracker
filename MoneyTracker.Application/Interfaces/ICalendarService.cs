using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Calendar;

namespace MoneyTracker.Application.Interfaces
{
    public interface ICalendarService
    {
        Task<OperationResult<Dictionary<int, List<CalendarEventDto>>>> GetMonthEventsAsync(int year, int month);
    }
}
