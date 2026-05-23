namespace MoneyTracker.Domain.Interfaces
{
    public interface ITenantOwned
    {
        Guid TenantId { get; set; }
    }
}
