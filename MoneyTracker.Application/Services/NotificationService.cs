using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Notifications;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(INotificationRepository repository, ILogger<NotificationService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OperationResult<List<NotificationDto>>> GetRecentAsync(int count = 20)
        {
            try
            {
                var items = await _repository.GetRecentAsync(count);
                var dtos = items.Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    Link = n.Link,
                    CreatedAt = n.CreatedAt
                }).ToList();
                return OperationResult<List<NotificationDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications");
                return OperationResult<List<NotificationDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<int>> GetUnreadCountAsync()
        {
            try
            {
                var count = await _repository.GetUnreadCountAsync();
                return OperationResult<int>.Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unread notification count");
                return OperationResult<int>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> MarkReadAsync(int id)
        {
            try
            {
                await _repository.MarkReadAsync(id);
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {Id} as read", id);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> MarkAllReadAsync()
        {
            try
            {
                await _repository.MarkAllReadAsync();
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
