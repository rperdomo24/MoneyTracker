using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;

namespace MoneyTracker.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<AppNotification>> GetRecentAsync(int count = 20);
        Task<int> GetUnreadCountAsync();
        Task MarkReadAsync(int id);
        Task MarkAllReadAsync();
        Task AddAsync(AppNotification notification);
        Task<bool> ExistsByDuplicateKeyTodayAsync(string duplicateKey);
        Task<bool> ExistsByDuplicateKeyAsync(string duplicateKey);
        Task<List<AppNotification>> GetUnreadByTypesAsync(IEnumerable<NotificationType> types);
    }
}
