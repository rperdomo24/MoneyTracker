using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class AiTextImportCache : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string InputHash { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
