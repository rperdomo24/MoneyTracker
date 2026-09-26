using Hangfire;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Infrastructure.Jobs;

namespace MoneyTracker.Infrastructure.Integrations.Gmail
{
    public class HangfireEmailSyncScheduler : IEmailSyncScheduler
    {
        public void EnableAutoSync(Guid tenantId, int intervalMinutes)
        {
            var interval = Math.Max(intervalMinutes, 15);
            var cron = $"*/{interval} * * * *";
            RecurringJob.AddOrUpdate<EmailSyncJob>(JobId(tenantId), job => job.ExecuteForTenantAsync(tenantId), cron);
        }

        public void DisableAutoSync(Guid tenantId) => RecurringJob.RemoveIfExists(JobId(tenantId));

        private static string JobId(Guid tenantId) => $"email-sync-{tenantId}";
    }
}
