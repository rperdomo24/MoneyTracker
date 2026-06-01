using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Budgets;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly IBudgetRepository _budgetRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<BudgetService> _logger;
        private readonly ITimeZoneService _timeZoneService;

        public BudgetService(
            IBudgetRepository budgetRepository,
            ICategoryRepository categoryRepository,
            ITransactionRepository transactionRepository,
            ITimeZoneService timeZoneService,
            ILogger<BudgetService> logger)
        {
            _budgetRepository = budgetRepository;
            _categoryRepository = categoryRepository;
            _transactionRepository = transactionRepository;
            _timeZoneService = timeZoneService;
            _logger = logger;
        }

        public async Task<OperationResult<BudgetDto>> GetByIdAsync(int id)
        {
            try
            {
                var entity = await _budgetRepository.GetByIdAsync(id);
                if (entity == null)
                    return OperationResult<BudgetDto>.Fail(OperationMessages.NotFound);

                var dto = entity.MapToDto();
                return OperationResult<BudgetDto>.Ok(dto, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<BudgetDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> CreateOrUpdateAsync(BudgetDto dto)
        {
            try
            {
                // Validaciones mínimas (luego metemos FluentValidation)
                if (dto.CategoryId <= 0)
                    return OperationResult<bool>.Fail(ValidationMessages.Required);

                if (dto.Year <= 0 || dto.Month is < 1 or > 12)
                    return OperationResult<bool>.Fail("Invalid year/month.");

                if (dto.Amount < 0)
                    return OperationResult<bool>.Fail("Amount must be >= 0.");

                // Si ya existe budget para CategoryId+Year+Month (no deleted), actualiza
                var existing = await _budgetRepository.GetByCategoryMonthAsync(dto.CategoryId, dto.Year, dto.Month);

                if (existing == null)
                {
                    var entity = new Budget
                    {
                        CategoryId = dto.CategoryId,
                        Year = dto.Year,
                        Month = dto.Month,
                        Amount = dto.Amount,
                        IncludeChildren = dto.IncludeChildren,
                        RolloverEnabled = dto.RolloverEnabled,
                        RolloverMode = dto.RolloverMode,
                        PaycheckPeriod = dto.PaycheckPeriod,
                        CreatedAt = _timeZoneService.GetNowInUtc(),
                        UpdatedAt = _timeZoneService.GetNowInUtc(),
                        IsDeleted = false
                    };

                    await _budgetRepository.AddAsync(entity);
                    return OperationResult<bool>.Ok(true, OperationMessages.Created);
                }
                else
                {
                    existing.Amount = dto.Amount;
                    existing.IncludeChildren = dto.IncludeChildren;
                    existing.RolloverEnabled = dto.RolloverEnabled;
                    existing.RolloverMode = dto.RolloverMode;
                    existing.PaycheckPeriod = dto.PaycheckPeriod;
                    existing.UpdatedAt = _timeZoneService.GetNowInUtc();

                    await _budgetRepository.UpdateAsync(existing);
                    return OperationResult<bool>.Ok(true, OperationMessages.Updated);
                }
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
                await _budgetRepository.SoftDeleteAsync(id);
                return OperationResult<bool>.Ok(true, OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<List<BudgetWithUsageDto>>> GetMonthlyWithUsageAsync(int year, int month)
        {
            try
            {
                var budgets = await _budgetRepository.GetByMonthAsync(year, month);

                var localStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                var localEnd = localStart.AddMonths(1).AddTicks(-1);

                var fromUtc = _timeZoneService.ConvertToUtc(localStart);
                var toUtc = _timeZoneService.ConvertToUtc(localEnd);

                var categories = await _categoryRepository.GetAllAsync(includeChildren: true, incluideSystem: true);

                categories = categories
                    .Where(c => !c.IsDeleted && c.Type != CategoryTypeEnum.Transfer)
                    .ToList();

                var categoriesById = categories.ToDictionary(c => c.Id);

                var byParent = categories
                    .Where(c => c.ParentId.HasValue)
                    .GroupBy(c => c.ParentId!.Value)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

                var tx = await _transactionRepository.GetFilteredAsync(
                    fromUtc,
                    toUtc,
                    new List<int>(),
                    new List<int>()
                );

                var usedByCategoryId = new Dictionary<int, decimal>();

                foreach (var t in tx)
                {
                    if (t.IsDeleted) continue;

                    var categoryId = t.CategoryId;

                    if (!categoriesById.TryGetValue(categoryId, out var cat))
                        continue;

                    decimal used = cat.Type == CategoryTypeEnum.Expense
                        ? Math.Abs(t.Amount)
                        : t.Amount;

                    if (usedByCategoryId.TryGetValue(categoryId, out var acc))
                        usedByCategoryId[categoryId] = acc + used;
                    else
                        usedByCategoryId[categoryId] = used;
                }

                var budgetByCategoryId = budgets
                    .Where(b => !b.IsDeleted)
                    .ToDictionary(b => b.CategoryId, b => b);

                var descendantsCache = new Dictionary<int, List<int>>();

                List<int> GetDescendants(int categoryId)
                {
                    if (descendantsCache.TryGetValue(categoryId, out var cached))
                        return cached;

                    var result = new List<int>();
                    var stack = new Stack<int>();
                    stack.Push(categoryId);

                    while (stack.Count > 0)
                    {
                        var current = stack.Pop();
                        if (!byParent.TryGetValue(current, out var children)) continue;

                        foreach (var childId in children)
                        {
                            result.Add(childId);
                            stack.Push(childId);
                        }
                    }

                    descendantsCache[categoryId] = result;
                    return result;
                }

                var response = new List<BudgetWithUsageDto>(categories.Count);

                foreach (var cat in categories)
                {
                    var hasBudget = budgetByCategoryId.TryGetValue(cat.Id, out var b);

                    var budgetDto = hasBudget
                        ? b!.MapToDto()
                        : new BudgetDto
                        {
                            Id = 0, 
                            CategoryId = cat.Id,
                            Year = year,
                            Month = month,
                            Amount = 0,
                            IncludeChildren = false,
                            RolloverEnabled = false,
                            RolloverMode = MoneyTracker.Domain.Enums.Budgets.RolloverMode.None
                        };

                    usedByCategoryId.TryGetValue(cat.Id, out var directUsed);
                    var used = directUsed;
                    if (hasBudget && b!.IncludeChildren)
                    {
                        foreach (var childId in GetDescendants(cat.Id))
                        {
                            if (usedByCategoryId.TryGetValue(childId, out var childUsed))
                                used += childUsed;
                        }
                    }

                    response.Add(new BudgetWithUsageDto
                    {
                        Budget = budgetDto,
                        Category = cat.MapToDto(),
                        Used = used,
                        DirectUsed = directUsed
                    });
                }

                response = response
                    .OrderBy(x => x.ParentCategoryId.HasValue ? 1 : 0)
                    .ThenBy(x => x.CategoryName)
                    .ToList();

                return OperationResult<List<BudgetWithUsageDto>>.Ok(response, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<BudgetWithUsageDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<int>> CopyMonthAsync(int fromYear, int fromMonth, int toYear, int toMonth)
        {
            try
            {
                var budgets = await _budgetRepository.GetByMonthAsync(fromYear, fromMonth);
                var active = budgets.Where(b => !b.IsDeleted).ToList();

                int copied = 0;
                foreach (var b in active)
                {
                    var existing = await _budgetRepository.GetByCategoryMonthAsync(b.CategoryId, toYear, toMonth);
                    if (existing != null) continue;

                    await _budgetRepository.AddAsync(new Budget
                    {
                        CategoryId = b.CategoryId,
                        Year = toYear,
                        Month = toMonth,
                        Amount = b.Amount,
                        IncludeChildren = b.IncludeChildren,
                        RolloverEnabled = b.RolloverEnabled,
                        RolloverMode = b.RolloverMode,
                        CreatedAt = _timeZoneService.GetNowInUtc(),
                        UpdatedAt = _timeZoneService.GetNowInUtc(),
                        IsDeleted = false
                    });
                    copied++;
                }

                var toLabel = new DateTime(toYear, toMonth, 1).ToString("MMMM yyyy");
                return OperationResult<int>.Ok(copied, $"{copied} budget(s) copied to {toLabel}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<int>.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}

