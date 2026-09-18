using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class AiRangeReportCache : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
