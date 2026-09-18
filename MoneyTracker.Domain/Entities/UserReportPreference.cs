using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class UserReportPreference : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string CategoriesJson { get; set; } = "[]";
        public string? ActiveCardFilter { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
