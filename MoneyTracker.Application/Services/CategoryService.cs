using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;
        private readonly IValidator<CategoryDto> _validator;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ICategoryRepository repository, IValidator<CategoryDto> validator, ILogger<CategoryService> logger)
        {
            _repository = repository;
            _validator = validator;
            _logger = logger;
        }

        public async Task<OperationResult<List<CategoryDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            var result = entities.Select(e => e.MapToDto()).ToList();
            return OperationResult<List<CategoryDto>>.Ok(result);
        }

        public async Task<OperationResult<CategoryDto?>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return OperationResult<CategoryDto?>.Fail(OperationMessages.NotFound);

            return OperationResult<CategoryDto?>.Ok(entity.MapToDto());
        }

        public async Task<OperationResult<bool>> CreateAsync(CategoryDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(validation.Errors.First().ErrorMessage);

            if (await _repository.ExistsAsync(dto.Name))
                return OperationResult<bool>.Fail(OperationMessages.AlreadyExists);

            try
            {
                await _repository.AddAsync(dto.MapToEntity());
                return OperationResult<bool>.Ok(true, OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> UpdateAsync(CategoryDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(validation.Errors.First().ErrorMessage);

            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing is null)
                return OperationResult<bool>.Fail(OperationMessages.NotFound);

            if (await _repository.ExistsAsync(dto.Name, dto.Id))
                return OperationResult<bool>.Fail(OperationMessages.AlreadyExists);

            try
            {
                existing.UpdateEntity(dto);
                await _repository.UpdateAsync(existing);
                return OperationResult<bool>.Ok(true, OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> DeleteAsync(int id)
        {
            try
            {
                await _repository.DeleteAsync(id);
                return OperationResult<bool>.Ok(true, OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
