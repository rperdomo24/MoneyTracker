using MoneyTracker.Application.DTOs.Goals;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers.Goals
{
    public static class SavingsGoalMapper
    {
        private const int DaysPerMonth = 30;

        public static SavingsGoalDto MapToDto(this SavingsGoal goal, DateTime todayLocal)
        {
            var current = goal.Contributions.Sum(c => c.Amount);
            var percent = goal.TargetAmount > 0
                ? (int)Math.Clamp(Math.Round(current / goal.TargetAmount * 100m, 0), 0m, 100m)
                : 0;
            var amountRemaining = Math.Max(0m, goal.TargetAmount - current);
            int? daysRemaining = goal.TargetDate.HasValue
                ? (goal.TargetDate.Value.Date - todayLocal.Date).Days
                : null;
            var isOpen = !goal.IsCompleted && amountRemaining > 0;
            var isOverdue = isOpen && daysRemaining.HasValue && daysRemaining.Value < 0;
            // With a month or less left the whole remaining amount is due, so a "per month" pace would only mislead.
            decimal? monthlyPace = isOpen && daysRemaining.HasValue && daysRemaining.Value > DaysPerMonth
                ? Math.Ceiling(amountRemaining / (daysRemaining.Value / (decimal)DaysPerMonth))
                : null;

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
                AmountRemaining = amountRemaining,
                DaysRemaining = daysRemaining,
                IsOverdue = isOverdue,
                MonthlyPaceNeeded = monthlyPace,
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
