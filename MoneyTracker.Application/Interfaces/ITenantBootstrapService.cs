namespace MoneyTracker.Application.Interfaces
{
    public interface ITenantBootstrapService
    {
        Task SeedDefaultsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    }
}
