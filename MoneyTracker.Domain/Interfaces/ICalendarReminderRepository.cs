using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface ICalendarReminderRepository
    {
        Task<List<CalendarReminder>> GetByMonthAsync(int year, int month);
        Task<CalendarReminder?> GetByIdAsync(int id);
        Task AddAsync(CalendarReminder entity);
        Task UpdateAsync(CalendarReminder entity);
        Task DeleteAsync(int id);
    }
}
