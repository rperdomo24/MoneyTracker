using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Goals;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers.Goals;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class SavingsGoalService : ISavingsGoalService
    {
        private readonly ISavingsGoalRepository _repo;
        private readonly ILogger<SavingsGoalService> _logger;

        public SavingsGoalService(ISavingsGoalRepository repo, ILogger<SavingsGoalService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<OperationResult<List<SavingsGoalDto>>> GetAllAsync()
        {
            try
            {
                var goals = await _repo.GetAllAsync();
                return OperationResult<List<SavingsGoalDto>>.Ok(goals.Select(g => g.MapToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<SavingsGoalDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<SavingsGoalDto>> GetByIdAsync(int id)
        {
            try
            {
                var goal = await _repo.GetByIdAsync(id);
                if (goal is null)
                    return OperationResult<SavingsGoalDto>.Fail(OperationMessages.NotFound);

                return OperationResult<SavingsGoalDto>.Ok(goal.MapToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<SavingsGoalDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> CreateAsync(SavingsGoalDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult.Fail("Name is required.");

                if (dto.TargetAmount <= 0)
                    return OperationResult.Fail("Target amount must be greater than zero.");

                var entity = dto.MapToEntity();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                if (entity.TargetDate.HasValue)
                    entity.TargetDate = DateTime.SpecifyKind(entity.TargetDate.Value, DateTimeKind.Utc);

                await _repo.AddAsync(entity);
                return OperationResult.Ok(OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> UpdateAsync(SavingsGoalDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult.Fail("Name is required.");

                if (dto.TargetAmount <= 0)
                    return OperationResult.Fail("Target amount must be greater than zero.");

                var entity = await _repo.GetByIdAsync(dto.Id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                entity.Name = dto.Name.Trim();
                entity.TargetAmount = dto.TargetAmount;
                entity.TargetDate = dto.TargetDate.HasValue
                    ? DateTime.SpecifyKind(dto.TargetDate.Value, DateTimeKind.Utc)
                    : null;
                entity.Color = dto.Color;
                entity.Notes = dto.Notes;
                entity.ContributionReminderDay = dto.ContributionReminderDay is >= 1 and <= 31 ? dto.ContributionReminderDay : null;
                entity.ContributionReminderAmount = dto.ContributionReminderAmount > 0 ? dto.ContributionReminderAmount : null;
                entity.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(entity);
                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            try
            {
                await _repo.SoftDeleteAsync(id);
                return OperationResult.Ok(OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> AddContributionAsync(SavingsContributionDto dto)
        {
            try
            {
                if (dto.Amount <= 0)
                    return OperationResult.Fail("Contribution amount must be greater than zero.");

                var goal = await _repo.GetByIdAsync(dto.GoalId);
                if (goal is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                var contribution = new SavingsContribution
                {
                    GoalId = dto.GoalId,
                    Amount = dto.Amount,
                    Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
                    Notes = dto.Notes,
                    LinkedTransactionId = dto.LinkedTransactionId,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddContributionAsync(contribution);
                return OperationResult.Ok(OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> UpdateContributionAsync(SavingsContributionDto dto)
        {
            try
            {
                if (dto.Amount <= 0)
                    return OperationResult.Fail("Contribution amount must be greater than zero.");

                var contribution = await _repo.GetContributionByIdAsync(dto.Id);
                if (contribution is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                contribution.Amount = dto.Amount;
                contribution.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
                contribution.Notes = dto.Notes;
                contribution.LinkedTransactionId = dto.LinkedTransactionId;

                await _repo.UpdateContributionAsync(contribution);
                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> DeleteContributionAsync(int contributionId)
        {
            try
            {
                await _repo.DeleteContributionAsync(contributionId);
                return OperationResult.Ok(OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> ToggleCompletedAsync(int id)
        {
            try
            {
                var goal = await _repo.GetByIdAsync(id);
                if (goal is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                goal.IsCompleted = !goal.IsCompleted;
                goal.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(goal);
                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
