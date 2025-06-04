using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _repository;
        private readonly IValidator<TransactionDto> _validator;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(ITransactionRepository repository, IValidator<TransactionDto> validator, ILogger<TransactionService> logger)
        {
            _repository = repository;
            _validator = validator;
            _logger = logger;
        }

        public async Task<OperationResult<List<TransactionDto>>> GetAllAsync()
        {
            try
            {
                var expenses = await _repository.GetAllAsync();
                var dtoList = expenses.Select(TransactionMapper.MapToDto).ToList();
                return OperationResult<List<TransactionDto>>.Ok(dtoList, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<TransactionDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<TransactionDto>> GetByIdAsync(int id)
        {
            try
            {
                var expense = await _repository.GetByIdAsync(id);
                if (expense == null)
                    return OperationResult<TransactionDto>.Fail(OperationMessages.NotFound);

                return OperationResult<TransactionDto>.Ok(TransactionMapper.MapToDto(expense), OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<TransactionDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> CreateAsync(TransactionDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            try
            {
                var entity = TransactionMapper.MapToEntity(dto);
                await _repository.AddAsync(entity);
                return OperationResult<bool>.Ok(true, OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> UpdateAsync(TransactionDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            try
            {
                var existing = await _repository.GetByIdAsync(dto.Id);
                if (existing is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                TransactionMapper.UpdateEntity(existing, dto);
                await _repository.UpdateAsync(existing);
                return OperationResult<bool>.Ok(true, OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> DeleteAsync(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                await _repository.DeleteAsync(id);
                return OperationResult<bool>.Ok(true, OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
