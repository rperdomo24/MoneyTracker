using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class CalendarReminder : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public string Color { get; set; } = "#6366f1";
        public bool IsRecurring { get; set; } = false;
        public RecurringFrequency? RecurrenceFrequency { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
