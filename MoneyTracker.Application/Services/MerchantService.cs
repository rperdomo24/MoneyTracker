using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Merchants;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class MerchantService : IMerchantService
    {
        private readonly IMerchantRepository _repository;
        private readonly ITimeZoneService _timeZoneService;
        private readonly ILogger<MerchantService> _logger;

        public MerchantService(
            IMerchantRepository repository,
            ITimeZoneService timeZoneService,
            ILogger<MerchantService> logger)
        {
            _repository = repository;
            _timeZoneService = timeZoneService;
            _logger = logger;
        }

        public async Task<OperationResult<List<MerchantDto>>> GetAllAsync()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                var counts = await _repository.GetTransactionCountsAsync();
                var dtos = entities.Select(m =>
                {
                    var dto = m.MapToDto();
                    dto.TransactionCount = counts.TryGetValue(m.Id, out var c) ? c : 0;
                    return dto;
                }).ToList();
                return OperationResult<List<MerchantDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<MerchantDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<MerchantDto>> GetByIdAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult<MerchantDto>.Fail(OperationMessages.NotFound);

                return OperationResult<MerchantDto>.Ok(entity.MapToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<MerchantDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> CreateAsync(MerchantDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult<bool>.Fail(ValidationMessages.Required);

                if (await _repository.ExistsAsync(dto.Name.Trim()))
                    return OperationResult<bool>.Fail("A merchant with this name already exists.");

                var entity = dto.MapToEntity();
                entity.CreatedAt = _timeZoneService.GetNowInUtc();
                entity.UpdatedAt = _timeZoneService.GetNowInUtc();

                await _repository.AddAsync(entity);
                return OperationResult<bool>.Ok(true, OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> UpdateAsync(MerchantDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return OperationResult<bool>.Fail(ValidationMessages.Required);

                var entity = await _repository.GetByIdAsync(dto.Id);
                if (entity is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                if (await _repository.ExistsAsync(dto.Name.Trim(), dto.Id))
                    return OperationResult<bool>.Fail("A merchant with this name already exists.");

                entity.Name = dto.Name.Trim();
                entity.UpdatedAt = _timeZoneService.GetNowInUtc();

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
                await _repository.SoftDeleteAsync(id);
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
