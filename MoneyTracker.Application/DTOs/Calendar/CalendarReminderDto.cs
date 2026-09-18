using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.DTOs.Calendar
{
    public class CalendarReminderDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public string Color { get; set; } = "#6366f1";
        public bool IsRecurring { get; set; } = false;
        public RecurringFrequency? RecurrenceFrequency { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
    }
}
