namespace MoneyTracker.Application.Interfaces
{
    public interface IEmailSyncScheduler
    {
        void EnableAutoSync(Guid tenantId, int intervalMinutes);
        void DisableAutoSync(Guid tenantId);
    }
}
