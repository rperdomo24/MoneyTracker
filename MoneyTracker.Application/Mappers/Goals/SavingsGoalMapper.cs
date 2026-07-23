using MoneyTracker.Application.DTOs.Goals;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers.Goals
{
    public static class SavingsGoalMapper
    {
        public static SavingsGoalDto MapToDto(this SavingsGoal goal)
        {
            var current = goal.Contributions.Sum(c => c.Amount);
            var percent = goal.TargetAmount > 0
                ? (int)Math.Clamp(Math.Round(current / goal.TargetAmount * 100m, 0), 0m, 100m)
                : 0;

            return new SavingsGoalDto
            {
                Id = goal.Id,
                Name = goal.Name,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = current,
                TargetDate = goal.TargetDate,
                Color = goal.Color,
                Notes = goal.Notes,
                ContributionReminderDay = goal.ContributionReminderDay,
                ContributionReminderAmount = goal.ContributionReminderAmount,
                IsCompleted = goal.IsCompleted,
                ProgressPercent = percent,
                CreatedAt = goal.CreatedAt,
                Contributions = goal.Contributions.Select(c => c.MapToDto()).ToList()
            };
        }

        public static SavingsGoal MapToEntity(this SavingsGoalDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name.Trim(),
            TargetAmount = dto.TargetAmount,
            TargetDate = dto.TargetDate,
            Color = dto.Color,
            Notes = dto.Notes,
            ContributionReminderDay = dto.ContributionReminderDay,
            ContributionReminderAmount = dto.ContributionReminderAmount,
            IsCompleted = dto.IsCompleted
        };

        public static SavingsContributionDto MapToDto(this SavingsContribution contribution) => new()
        {
            Id = contribution.Id,
            GoalId = contribution.GoalId,
            Amount = contribution.Amount,
            Date = contribution.Date,
            Notes = contribution.Notes,
            LinkedTransactionId = contribution.LinkedTransactionId,
            LinkedTransactionName = contribution.LinkedTransaction?.Name,
            LinkedTransactionAccount = contribution.LinkedTransaction?.Account?.Name,
            LinkedTransactionDate = contribution.LinkedTransaction?.Date,
            LinkedTransactionAmount = contribution.LinkedTransaction?.Amount
        };
    }
}
