using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Jobs
{
    public class EmailSyncJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailSyncJob> _logger;

        public EmailSyncJob(IServiceScopeFactory scopeFactory, ILogger<EmailSyncJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task ExecuteForTenantAsync(Guid tenantId)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var engine = scope.ServiceProvider.GetRequiredService<IEmailSyncEngine>();
            var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            try
            {
                var found = await engine.SyncTenantAsync(tenantId);

                if (found <= 0) return;

                var key = $"email-import-{tenantId}-{DateTime.UtcNow:yyyy-MM-dd-HH}";
                if (await notificationRepo.ExistsByDuplicateKeyAsync(key)) return;

                await notificationRepo.AddAsync(new AppNotification
                {
                    TenantId = tenantId,
                    Title = "New bank transaction emails found",
                    Message = $"{found} new email(s) look like bank transactions. Review them to import.",
                    Type = NotificationType.EmailTransactionsFound,
                    Link = "/settings#email-import",
                    DuplicateKey = key
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running scheduled Gmail sync for tenant {TenantId}", tenantId);
            }
        }
    }
}
