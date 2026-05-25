using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Notifications;

namespace MoneyTracker.Application.Interfaces
{
    public interface INotificationService
    {
        Task<OperationResult<List<NotificationDto>>> GetRecentAsync(int count = 20);
        Task<OperationResult<int>> GetUnreadCountAsync();
        Task<OperationResult> MarkReadAsync(int id);
        Task<OperationResult> MarkAllReadAsync();
    }
}
