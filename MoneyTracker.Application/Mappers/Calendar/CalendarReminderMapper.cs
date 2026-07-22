using MoneyTracker.Application.DTOs.Calendar;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers.Calendar
{
    public static class CalendarReminderMapper
    {
        public static CalendarReminderDto MapToDto(this CalendarReminder entity) =>
            new()
            {
                Id = entity.Id,
                Title = entity.Title,
                Date = entity.Date,
                Notes = entity.Notes,
                Color = entity.Color,
                IsRecurring = entity.IsRecurring,
                RecurrenceFrequency = entity.RecurrenceFrequency,
                RecurrenceEndDate = entity.RecurrenceEndDate
            };

        public static CalendarReminder MapToEntity(this CalendarReminderDto dto) =>
            new()
            {
                Id = dto.Id,
                Title = dto.Title,
                Date = dto.Date.ToUniversalTime(),
                Notes = dto.Notes,
                Color = dto.Color,
                IsRecurring = dto.IsRecurring,
                RecurrenceFrequency = dto.RecurrenceFrequency,
                RecurrenceEndDate = dto.RecurrenceEndDate
            };

        public static void UpdateEntity(this CalendarReminder entity, CalendarReminderDto dto)
        {
            entity.Title = dto.Title;
            entity.Date = dto.Date.ToUniversalTime();
            entity.Notes = dto.Notes;
            entity.Color = dto.Color;
            entity.IsRecurring = dto.IsRecurring;
            entity.RecurrenceFrequency = dto.RecurrenceFrequency;
            entity.RecurrenceEndDate = dto.RecurrenceEndDate;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
