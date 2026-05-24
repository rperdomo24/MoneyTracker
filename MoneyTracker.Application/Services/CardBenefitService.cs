using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.CardBenefits;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers.CardBenefits;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class CardBenefitService : ICardBenefitService
    {
        private readonly ICardBenefitRepository _repository;
        private readonly IValidator<CardBenefitDto> _validator;
        private readonly ILogger<CardBenefitService> _logger;

        public CardBenefitService(
            ICardBenefitRepository repository,
            IValidator<CardBenefitDto> validator,
            ILogger<CardBenefitService> logger)
        {
            _repository = repository;
            _validator = validator;
            _logger = logger;
        }

        public async Task<OperationResult<List<CardBenefitDto>>> GetAllAsync()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                var dtos = entities.Select(e => e.MapToDto()).ToList();
                return OperationResult<List<CardBenefitDto>>.Ok(dtos, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<CardBenefitDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<List<CardBenefitDto>>> GetByAccountAsync(int accountId)
        {
            try
            {
                var entities = await _repository.GetByAccountAsync(accountId);
                var dtos = entities.Select(e => e.MapToDto()).ToList();
                return OperationResult<List<CardBenefitDto>>.Ok(dtos, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<CardBenefitDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<CardBenefitDto>> GetByIdAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult<CardBenefitDto>.Fail(OperationMessages.NotFound);

                return OperationResult<CardBenefitDto>.Ok(entity.MapToDto(), OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<CardBenefitDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> CreateAsync(CardBenefitDto dto)
        {
            try
            {
                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return OperationResult<bool>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

                var entity = dto.MapToEntity();
                await _repository.AddAsync(entity);
                return OperationResult<bool>.Ok(true, OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> UpdateAsync(CardBenefitDto dto)
        {
            try
            {
                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return OperationResult<bool>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

                var entity = await _repository.GetByIdAsync(dto.Id);
                if (entity is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                entity.UpdateEntity(dto);
                await _repository.UpdateAsync(entity);
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
                var entity = await _repository.GetByIdAsync(id);
                if (entity is null)
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
