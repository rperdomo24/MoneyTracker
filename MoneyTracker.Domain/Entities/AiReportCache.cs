using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class AiReportCache : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
