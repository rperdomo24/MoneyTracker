namespace MoneyTracker.Application.DTOs.Goals
{
    public class SavingsGoalDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime? TargetDate { get; set; }
        public string Color { get; set; } = "#6366f1";
        public string? Notes { get; set; }
        public int? ContributionReminderDay { get; set; }
        public decimal? ContributionReminderAmount { get; set; }
        public bool IsCompleted { get; set; }
        public int ProgressPercent { get; set; }
        public List<SavingsContributionDto> Contributions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
