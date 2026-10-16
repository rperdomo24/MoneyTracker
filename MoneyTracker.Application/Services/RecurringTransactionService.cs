using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.RecurringTransactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class RecurringTransactionService : IRecurringTransactionService
    {
        private readonly IRecurringTransactionRepository _repository;
        private readonly ILogger<RecurringTransactionService> _logger;

        public RecurringTransactionService(
            IRecurringTransactionRepository repository,
            ILogger<RecurringTransactionService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OperationResult<List<RecurringTransactionDto>>> GetAllAsync()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return OperationResult<List<RecurringTransactionDto>>.Ok(
                    entities.Select(e => e.MapToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recurring transactions");
                return OperationResult<List<RecurringTransactionDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<RecurringTransactionDto>> GetByIdAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult<RecurringTransactionDto>.Fail(OperationMessages.NotFound);

                return OperationResult<RecurringTransactionDto>.Ok(entity.MapToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recurring transaction {Id}", id);
                return OperationResult<RecurringTransactionDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> CreateAsync(RecurringTransactionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return OperationResult.Fail("Name is required.");
            if (dto.Amount == 0)
                return OperationResult.Fail("Amount cannot be zero.");
            if (dto.AccountId <= 0)
                return OperationResult.Fail("Account is required.");
            if (dto.CategoryId <= 0)
                return OperationResult.Fail("Category is required.");
            if (dto.StartDate == default)
                return OperationResult.Fail("Start date is required.");
            if (dto.TotalOccurrences.HasValue && dto.TotalOccurrences.Value <= 0)
                return OperationResult.Fail("Total installments must be greater than zero.");
            if (dto.EndDate.HasValue && dto.EndDate.Value <= dto.StartDate)
                return OperationResult.Fail("End date must be after start date.");

            try
            {
                await _repository.AddAsync(dto.MapToEntity());
                return OperationResult.Ok(OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recurring transaction");
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> UpdateAsync(RecurringTransactionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return OperationResult.Fail("Name is required.");
            if (dto.Amount == 0)
                return OperationResult.Fail("Amount cannot be zero.");
            if (dto.TotalOccurrences.HasValue && dto.TotalOccurrences.Value <= 0)
                return OperationResult.Fail("Total installments must be greater than zero.");

            try
            {
                var entity = await _repository.GetByIdAsync(dto.Id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                entity.UpdateEntity(dto);
                await _repository.UpdateAsync(entity);
                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating recurring transaction {Id}", dto.Id);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                await _repository.DeleteAsync(id);
                return OperationResult.Ok(OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recurring transaction {Id}", id);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> SetStatusAsync(int id, RecurringStatus status)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                var message = GetTransitionMessage(entity.Status, status);
                if (message is null)
                    return OperationResult.Fail($"Cannot change a {entity.Status.ToString().ToLowerInvariant()} recurring transaction to {status.ToString().ToLowerInvariant()}.");

                entity.Status = status;
                entity.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(entity);
                return OperationResult.Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing status of recurring transaction {Id} to {Status}", id, status);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        // Allowed: Active <-> Paused, Active/Paused -> Completed, Completed -> Active (reopen). Null = not allowed.
        private static string? GetTransitionMessage(RecurringStatus from, RecurringStatus to) => (from, to) switch
        {
            (RecurringStatus.Active, RecurringStatus.Paused) => "Paused.",
            (RecurringStatus.Paused, RecurringStatus.Active) => "Resumed.",
            (RecurringStatus.Active or RecurringStatus.Paused, RecurringStatus.Completed) => "Marked as completed.",
            (RecurringStatus.Completed, RecurringStatus.Active) => "Reopened.",
            _ => null
        };
    }
}
