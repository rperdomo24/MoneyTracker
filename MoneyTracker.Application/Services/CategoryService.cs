using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Const;
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
                // incluideSystem=false hides system categories (Initial Balance, Balance
                // Adjustment, Transfer, etc.) — except Uncategorized, which stays visible like
                // a normal category since transactions dumped there still need to be reviewed
                // and re-categorized by the user.
                entities = entities.Where(x => !x.IsSystem || SystemCategoryCodes.IsUncategorized(x.SystemCategoryCode)).ToList();
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
            var (source, _, error) = await ValidateMergePairAsync(sourceId, targetId);
            if (error is not null)
                return OperationResult<bool>.Fail(error);

            try
            {
                var wasFullMerge = await _repository.MergeAsync(sourceId, targetId, transactionIdsToMove);
                return OperationResult<bool>.Ok(true, wasFullMerge ? OperationMessages.CategoryMerged : OperationMessages.CategoryPartiallyMoved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging category {SourceId} into {TargetId}", sourceId, targetId);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<CategoryMergePreviewDto>> GetMergePreviewAsync(int sourceId, int targetId)
        {
            var (source, _, error) = await ValidateMergePairAsync(sourceId, targetId);
            if (error is not null)
                return OperationResult<CategoryMergePreviewDto>.Fail(error);

            var budgetPreview = await _repository.GetBudgetMergePreviewAsync(sourceId, targetId);

            var preview = new CategoryMergePreviewDto
            {
                Budgets = budgetPreview
                    .Select(b => new BudgetMergePreviewItemDto
                    {
                        Year = b.Year,
                        Month = b.Month,
                        Amount = b.Amount,
                        WillBeDropped = b.WillBeDropped
                    })
                    .ToList(),
                SubcategoryNames = source!.Children.Select(c => c.Name).ToList()
            };

            return OperationResult<CategoryMergePreviewDto>.Ok(preview, OperationMessages.DataRetrieved);
        }

        private async Task<(Domain.Entities.Category? Source, Domain.Entities.Category? Target, string? Error)> ValidateMergePairAsync(int sourceId, int targetId)
        {
            if (sourceId == targetId)
                return (null, null, OperationMessages.CategoryMergeSelf);

            var source = await _repository.GetByIdAsync(sourceId);
            if (source is null)
                return (null, null, OperationMessages.NotFound);

            var target = await _repository.GetByIdAsync(targetId);
            if (target is null)
                return (null, null, OperationMessages.NotFound);

            if (source.IsSystem || target.IsSystem)
                return (null, null, OperationMessages.CategoryMergeSystem);

            if (source.Type != target.Type)
                return (null, null, OperationMessages.CategoryMergeDifferentType);

            return (source, target, null);
        }

        public async Task<OperationResult<bool>> DeleteAsync(int id)
        {
            try
            {
                var category = await _repository.GetByIdAsync(id);
                if (category is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                if (category.IsSystem)
                    return OperationResult<bool>.Fail(OperationMessages.CategoryDeleteSystem);

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
