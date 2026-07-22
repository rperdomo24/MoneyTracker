using MoneyTracker.Domain.Enums.Calendar;

namespace MoneyTracker.Application.DTOs.Calendar
{
    public class CalendarEventDto
    {
        public CalendarEventType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public decimal? Amount { get; set; }
        public string? Color { get; set; }
        public int? EntityId { get; set; }
        public string? NavigationUrl { get; set; }
    }
}
