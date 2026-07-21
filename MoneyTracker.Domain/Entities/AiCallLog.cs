using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class AiCallLog : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public DateTime CalledAtUtc { get; set; } = DateTime.UtcNow;
        public string Service { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int CachedTokens { get; set; }
        public bool WasCacheHit { get; set; }
        public long DurationMs { get; set; }
    }
}
