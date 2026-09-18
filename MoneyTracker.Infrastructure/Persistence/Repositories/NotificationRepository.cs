using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(IServiceScopeFactory scopeFactory, ILogger<NotificationRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<List<AppNotification>> GetRecentAsync(int count = 20)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.AppNotifications
                    .AsNoTracking()
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent notifications");
                return [];
            }
        }

        public async Task<int> GetUnreadCountAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.AppNotifications.CountAsync(n => !n.IsRead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unread notification count");
                return 0;
            }
        }

        public async Task MarkReadAsync(int id)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            var notification = await context.AppNotifications.FirstOrDefaultAsync(n => n.Id == id);
            if (notification is null) return;
            notification.IsRead = true;
            await context.SaveChangesAsync();
        }

        public async Task MarkAllReadAsync()
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            await context.AppNotifications
                .Where(n => !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        public async Task AddAsync(AppNotification notification)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            context.AppNotifications.Add(notification);
            await context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByDuplicateKeyTodayAsync(string duplicateKey)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                var today = DateTime.UtcNow.Date;
                return await context.AppNotifications
                    .IgnoreQueryFilters()
                    .AnyAsync(n => n.DuplicateKey == duplicateKey && n.CreatedAt.Date >= today);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking duplicate notification key");
                return false;
            }
        }

        public async Task<bool> ExistsByDuplicateKeyAsync(string duplicateKey)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.AppNotifications
                    .IgnoreQueryFilters()
                    .AnyAsync(n => n.DuplicateKey == duplicateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking duplicate notification key (any date)");
                return false;
            }
        }

        public async Task<List<AppNotification>> GetUnreadByTypesAsync(IEnumerable<NotificationType> types)
        {
            try
            {
                var typeList = types.ToList();
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.AppNotifications
                    .AsNoTracking()
                    .Where(n => !n.IsRead && typeList.Contains(n.Type))
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unread notifications by type");
                return [];
            }
        }
    }
}
