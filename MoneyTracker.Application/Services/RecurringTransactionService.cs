using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.RecurringTransactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
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

        public async Task<OperationResult> ToggleActiveAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                entity.IsActive = !entity.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(entity);
                return OperationResult.Ok(entity.IsActive ? "Activated." : "Paused.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling recurring transaction {Id}", id);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
