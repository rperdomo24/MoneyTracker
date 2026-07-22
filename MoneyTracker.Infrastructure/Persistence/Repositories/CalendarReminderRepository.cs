using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class CalendarReminderRepository : ICalendarReminderRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CalendarReminderRepository> _logger;

        public CalendarReminderRepository(IServiceScopeFactory scopeFactory, ILogger<CalendarReminderRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<List<CalendarReminder>> GetByMonthAsync(int year, int month)
        {
            try
            {
                var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                // Fetch all non-deleted reminders and those whose recurrence hasn't ended yet
                return await context.CalendarReminders
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted &&
                                (r.Date <= monthEnd) &&
                                (!r.RecurrenceEndDate.HasValue || r.RecurrenceEndDate.Value >= monthStart))
                    .OrderBy(r => r.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calendar reminders for {Year}/{Month}", year, month);
                return [];
            }
        }

        public async Task<CalendarReminder?> GetByIdAsync(int id)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.CalendarReminders.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calendar reminder {Id}", id);
                return null;
            }
        }

        public async Task AddAsync(CalendarReminder entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.CalendarReminders.Add(entity);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CalendarReminder entity)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.CalendarReminders.Update(entity);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            var entity = await context.CalendarReminders.FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null) return;
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
}
