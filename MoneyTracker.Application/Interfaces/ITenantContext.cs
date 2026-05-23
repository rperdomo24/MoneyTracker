namespace MoneyTracker.Application.Interfaces
{
    public interface ITenantContext
    {
        Guid? TenantId { get; }
    }
}
