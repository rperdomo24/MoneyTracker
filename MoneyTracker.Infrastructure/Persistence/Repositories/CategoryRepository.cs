using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(IServiceScopeFactory scopeFactory, ILogger<CategoryRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                return await context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.Name)
                    .Include(c => c.Children)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories.");
                return new();
            }
        }

        public async Task<List<Category>> GetAllAsync(bool includeChildren, bool incluideSystem)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                IQueryable<Category> query = context.Categories
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted);

                if (!incluideSystem)
                    query = query.Where(c => !c.IsSystem);

                // Para budgets NO necesitas Include.
                // Si lo querés conservar para otras pantallas, ok:
                if (includeChildren)
                    query = query.Include(c => c.Children);

                query = query
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.ParentId)   // ayuda cuando lo agrupas por parent
                    .ThenBy(c => c.Name);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories.");
                return new();
            }
        }


        public async Task<List<Category>> GetAllWithSubcategoriesAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                return await context.Categories
                    .Include(c => c.Children)
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.Name)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories with subcategories.");
                return new();
            }
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                return await context.Categories
                    .Include(c => c.Children)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by ID {Id}.", id);
                return null;
            }
        }

        public async Task AddAsync(Category category)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                context.Categories.Add(category);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding category.");
                throw;
            }
        }

        public async Task UpdateAsync(Category category)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                context.Categories.Update(category);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category.");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                var entity = await context.Categories.FindAsync(id);
                if (entity is null)
                    return;

                // Every transaction that belonged to this category moves to Uncategorized
                // (matching Income/Expense) so it stays selectable/reviewable — it must never
                // point at a now-hidden category.
                var affectedTransactions = await context.Transaction
                    .Where(t => t.CategoryId == id)
                    .ToListAsync();

                if (affectedTransactions.Count > 0)
                {
                    var uncategorizedCode = entity.Type == Domain.Enums.Category.CategoryTypeEnum.Income
                        ? Domain.Const.SystemCategoryCodes.UncategorizedIncome
                        : Domain.Const.SystemCategoryCodes.UncategorizedExpense;

                    var uncategorizedId = await context.Categories
                        .Where(c => c.IsSystem && c.SystemCategoryCode == uncategorizedCode)
                        .Select(c => c.Id)
                        .FirstOrDefaultAsync();

                    if (uncategorizedId <= 0)
                        throw new InvalidOperationException($"Uncategorized system category not found for code '{uncategorizedCode}'.");

                    foreach (var t in affectedTransactions)
                        t.CategoryId = uncategorizedId;
                }

                // Subcategories lose their parent (become top-level) instead of moving anywhere
                // — the parent "no longer exists" once soft-deleted, so nothing should still
                // claim to be its child.
                var children = await context.Categories
                    .Where(c => c.ParentId == id)
                    .ToListAsync();
                foreach (var child in children)
                    child.ParentId = null;

                // Soft delete only — the row keeps existing, so it never conflicts with the
                // Transaction/Budget FKs and stays recoverable. Never hard-remove here.
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID {Id}.", id);
                throw;
            }
        }

        public async Task<bool> HasBudgetsAsync(int categoryId)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                return await context.Budgets
                    .AsNoTracking()
                    .AnyAsync(b => b.CategoryId == categoryId && !b.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking budgets for category with ID {Id}.", categoryId);
                return false;
            }
        }

        public async Task<Dictionary<int, int>> GetTransactionCountsByCategoryAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                return await context.Transaction
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted)
                    .GroupBy(t => t.CategoryId)
                    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction counts by category.");
                return new Dictionary<int, int>();
            }
        }

        public async Task<HashSet<int>> GetUsedCategoryIdsAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                var transactionCategoryIds = await context.Transaction
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted)
                    .Select(t => t.CategoryId)
                    .Distinct()
                    .ToListAsync();

                var budgetCategoryIds = await context.Budgets
                    .AsNoTracking()
                    .Where(b => !b.IsDeleted)
                    .Select(b => b.CategoryId)
                    .Distinct()
                    .ToListAsync();

                return transactionCategoryIds.Concat(budgetCategoryIds).ToHashSet();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting used category IDs.");
                return new HashSet<int>();
            }
        }

        public async Task<bool> MergeAsync(int sourceId, int targetId, IReadOnlyCollection<int>? transactionIdsToMove = null)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                await using var transaction = await context.Database.BeginTransactionAsync();

                // transactionIdsToMove == null means "move everything" (legacy/full merge).
                var allSourceTransactions = await context.Transaction
                    .Where(t => t.CategoryId == sourceId)
                    .ToListAsync();

                var transactionsToMove = transactionIdsToMove is null
                    ? allSourceTransactions
                    : allSourceTransactions.Where(t => transactionIdsToMove.Contains(t.Id)).ToList();

                // Only a FULL merge (every transaction moved, nothing left behind) consolidates
                // budgets/subcategories and deletes the source category. A PARTIAL move — the
                // user left some transactions unchecked — just reassigns those and leaves the
                // source category (its budgets, subcategories, and remaining transactions)
                // completely untouched. Deleting the source on a partial move would destroy a
                // category the user explicitly chose to keep.
                var isFullMerge = transactionsToMove.Count == allSourceTransactions.Count;

                foreach (var t in transactionsToMove)
                    t.CategoryId = targetId;

                if (!isFullMerge)
                {
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return false;
                }

                var activeSourceBudgets = await context.Budgets
                    .Where(b => b.CategoryId == sourceId && !b.IsDeleted)
                    .ToListAsync();

                var targetBudgetKeys = await context.Budgets
                    .Where(b => b.CategoryId == targetId && !b.IsDeleted)
                    .Select(b => new { b.Year, b.Month })
                    .ToListAsync();
                var targetKeySet = targetBudgetKeys.Select(k => (k.Year, k.Month)).ToHashSet();

                foreach (var budget in activeSourceBudgets)
                {
                    if (targetKeySet.Contains((budget.Year, budget.Month)))
                    {
                        // Same-month budget already exists on the target: (CategoryId, Year, Month)
                        // is unique, so the source duplicate is soft-deleted instead of moved.
                        // Never hard-delete.
                        budget.IsDeleted = true;
                        budget.DeletedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        budget.CategoryId = targetId;
                    }
                }

                var children = await context.Categories
                    .Where(c => c.ParentId == sourceId)
                    .ToListAsync();
                foreach (var child in children)
                    child.ParentId = targetId;

                // Soft delete only — the source row stays (with any already soft-deleted
                // budgets it still holds), so this can never conflict with the Budget Restrict
                // FK. Never hard-remove the category.
                var source = await context.Categories.FindAsync(sourceId);
                if (source is not null)
                {
                    source.IsDeleted = true;
                    source.DeletedAt = DateTime.UtcNow;
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging category {SourceId} into {TargetId}.", sourceId, targetId);
                throw;
            }
        }

        public async Task<List<(int Year, int Month, decimal Amount, bool WillBeDropped)>> GetBudgetMergePreviewAsync(int sourceId, int targetId)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                var sourceBudgets = await context.Budgets
                    .AsNoTracking()
                    .Where(b => b.CategoryId == sourceId && !b.IsDeleted)
                    .Select(b => new { b.Year, b.Month, b.Amount })
                    .ToListAsync();

                var targetKeySet = await context.Budgets
                    .AsNoTracking()
                    .Where(b => b.CategoryId == targetId && !b.IsDeleted)
                    .Select(b => new { b.Year, b.Month })
                    .ToListAsync();
                var targetKeys = targetKeySet.Select(k => (k.Year, k.Month)).ToHashSet();

                return sourceBudgets
                    .Select(b => (b.Year, b.Month, b.Amount, WillBeDropped: targetKeys.Contains((b.Year, b.Month))))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting budget merge preview for category {SourceId} into {TargetId}.", sourceId, targetId);
                return new List<(int Year, int Month, decimal Amount, bool WillBeDropped)>();
            }
        }

        public async Task<bool> ExistsAsync(string name, int? excludeId = null)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                return await context.Categories
                    .AsNoTracking() // Performance para consultas de solo lectura
                    .AnyAsync(c => c.Name == name && (!excludeId.HasValue || c.Id != excludeId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of category with name {Name}.", name);
                return false;
            }
        }
    }
}
