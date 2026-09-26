namespace MoneyTracker.Application.Interfaces
{
    public interface IEmailSyncEngine
    {
        /// <summary>Runs the full sync pipeline (rules -> dedupe -> AI -> fingerprint dedupe) for one tenant. Returns how many new Pending items were found.</summary>
        Task<int> SyncTenantAsync(Guid tenantId);
    }
}
