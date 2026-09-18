using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class SavingsGoal : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public DateTime? TargetDate { get; set; }
        public string Color { get; set; } = "#6366f1";
        public string? Notes { get; set; }
        public int? ContributionReminderDay { get; set; }
        public decimal? ContributionReminderAmount { get; set; }
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public ICollection<SavingsContribution> Contributions { get; set; } = new List<SavingsContribution>();
    }
}
