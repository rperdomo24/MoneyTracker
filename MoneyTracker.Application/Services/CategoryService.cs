using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Interfaces;
using System.Globalization;
using System.Text;

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

        public async Task<OperationResult<List<CategoryDto>>> GetAllWithChildAsync(bool incluideSystem = true)
        {
            var entities = await _repository.GetAllAsync(includeChildren: true, incluideSystem: true);

            if (!incluideSystem)
            {
                entities = entities.Where(x => x.Type != CategoryTypeEnum.Transfer).ToList();
            }

            var result = entities
                .Select(e => e.MapToDto()).ToList();

            var usedIds = await _repository.GetUsedCategoryIdsAsync() ?? new HashSet<int>();
            var transactionCounts = await _repository.GetTransactionCountsByCategoryAsync() ?? new Dictionary<int, int>();
            MarkUnused(result, usedIds, transactionCounts);
            MarkPossibleDuplicates(result);

            return OperationResult<List<CategoryDto>>.Ok(result);
        }

        private static void MarkUnused(List<CategoryDto> categories, HashSet<int> usedIds, Dictionary<int, int> transactionCounts)
        {
            foreach (var category in categories)
            {
                category.IsUnused = !category.IsSystem && !usedIds.Contains(category.Id);
                category.TransactionCount = transactionCounts.GetValueOrDefault(category.Id);
                MarkUnused(category.Children, usedIds, transactionCounts);
            }
        }

        // Heuristic only: flags categories whose normalized name matches another one
        // at the same level (top-level by Type, subcategories by parent). Manual review
        // still required — this cannot detect semantic duplicates (e.g. "Comida" vs "Alimentación").
        private static void MarkPossibleDuplicates(List<CategoryDto> categories)
        {
            var topLevelGroups = categories
                .GroupBy(c => (c.Type, Name: NormalizeName(c.Name)));

            foreach (var group in topLevelGroups)
            {
                var isDuplicate = group.Count() > 1;
                foreach (var category in group)
                    category.IsPossibleDuplicate = isDuplicate;
            }

            foreach (var category in categories)
            {
                var childGroups = category.Children.GroupBy(c => NormalizeName(c.Name));
                foreach (var group in childGroups)
                {
                    var isDuplicate = group.Count() > 1;
                    foreach (var child in group)
                        child.IsPossibleDuplicate = isDuplicate;
                }
            }
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var withoutDiacritics = RemoveDiacritics(name.Trim().ToLowerInvariant());
            return string.Concat(withoutDiacritics.Where(char.IsLetterOrDigit));
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
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

        public async Task<OperationResult<bool>> MergeAsync(int sourceId, int targetId, List<int>? transactionIdsToMove = null)
        {
            if (sourceId == targetId)
                return OperationResult<bool>.Fail(OperationMessages.CategoryMergeSelf);

            var source = await _repository.GetByIdAsync(sourceId);
            if (source is null)
                return OperationResult<bool>.Fail(OperationMessages.NotFound);

            var target = await _repository.GetByIdAsync(targetId);
            if (target is null)
                return OperationResult<bool>.Fail(OperationMessages.NotFound);

            if (source.IsSystem || target.IsSystem)
                return OperationResult<bool>.Fail(OperationMessages.CategoryMergeSystem);

            if (source.Type != target.Type)
                return OperationResult<bool>.Fail(OperationMessages.CategoryMergeDifferentType);

            try
            {
                await _repository.MergeAsync(sourceId, targetId, transactionIdsToMove);
                return OperationResult<bool>.Ok(true, OperationMessages.CategoryMerged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging category {SourceId} into {TargetId}", sourceId, targetId);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> DeleteAsync(int id)
        {
            try
            {
                if (await _repository.HasBudgetsAsync(id))
                    return OperationResult<bool>.Fail(OperationMessages.CategoryHasBudgets);

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
