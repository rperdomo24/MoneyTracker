using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Calendar;

namespace MoneyTracker.Application.Interfaces
{
    public interface ICalendarReminderService
    {
        Task<OperationResult<List<CalendarReminderDto>>> GetByMonthAsync(int year, int month);
        Task<OperationResult> CreateAsync(CalendarReminderDto dto);
        Task<OperationResult> UpdateAsync(CalendarReminderDto dto);
        Task<OperationResult> DeleteAsync(int id);
    }
}
