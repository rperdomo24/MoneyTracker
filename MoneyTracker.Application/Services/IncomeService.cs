using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class IncomeService : IIncomeService
    {
        private readonly IIncomeRepository _repository;
        private readonly ILogger<IncomeService> _logger;

        public IncomeService(IIncomeRepository repository, ILogger<IncomeService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OperationResult<List<IncomeDto>>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            var result = list.Select(x => x.MapToDto()).ToList();
            return OperationResult<List<IncomeDto>>.Ok(result);
        }

        public async Task<OperationResult<IncomeDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity is null)
                return OperationResult<IncomeDto>.Fail(OperationMessages.NotFound);

            return OperationResult<IncomeDto>.Ok(entity.MapToDto());
        }

        public async Task<OperationResult> AddAsync(IncomeDto dto)
        {
            try
            {
                var entity = dto.MapToEntity();
                var success = await _repository.AddAsync(entity);

                return success
                    ? OperationResult.Ok(OperationMessages.Created)
                    : OperationResult.Fail(OperationMessages.UnexpectedError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding income: {@IncomeDto}", dto);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> UpdateAsync(IncomeDto dto)
        {
            try
            {
                var entity = dto.MapToEntity();
                var success = await _repository.UpdateAsync(entity);

                return success
                    ? OperationResult.Ok(OperationMessages.Updated)
                    : OperationResult.Fail(OperationMessages.UnexpectedError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating income: {@IncomeDto}", dto);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            try
            {
                var success = await _repository.DeleteAsync(id);

                return success
                    ? OperationResult.Ok(OperationMessages.Deleted)
                    : OperationResult.Fail(OperationMessages.UnexpectedError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting income with id: {Id}", id);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
