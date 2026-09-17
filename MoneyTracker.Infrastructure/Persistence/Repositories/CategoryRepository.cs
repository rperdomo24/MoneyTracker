using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(MoneyTrackerDbContext context, ILogger<CategoryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            try
            {
                return await _context.Categories
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
                IQueryable<Category> query = _context.Categories
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
                return await _context.Categories
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
                return await _context.Categories
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
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
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
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
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
                var entity = await _context.Categories.FindAsync(id);
                if (entity is null)
                    return;

                _context.Categories.Remove(entity);
                await _context.SaveChangesAsync();
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
                return await _context.Budgets
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
                return await _context.Transaction
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
                var transactionCategoryIds = await _context.Transaction
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted)
                    .Select(t => t.CategoryId)
                    .Distinct()
                    .ToListAsync();

                var budgetCategoryIds = await _context.Budgets
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

        public async Task MergeAsync(int sourceId, int targetId, IReadOnlyCollection<int>? transactionIdsToMove = null)
        {
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                // transactionIdsToMove == null means "move everything" (legacy/full merge).
                // Transactions left out of the set keep pointing at the source category and,
                // once it's deleted below, fall back to Uncategorized via the SetNull FK.
                var candidateTransactions = _context.Transaction.Where(t => t.CategoryId == sourceId);
                var transactionsToMove = await (transactionIdsToMove is null
                        ? candidateTransactions
                        : candidateTransactions.Where(t => transactionIdsToMove.Contains(t.Id)))
                    .ToListAsync();
                foreach (var t in transactionsToMove)
                    t.CategoryId = targetId;

                var sourceBudgets = await _context.Budgets
                    .Where(b => b.CategoryId == sourceId && !b.IsDeleted)
                    .ToListAsync();

                var targetBudgetKeys = await _context.Budgets
                    .Where(b => b.CategoryId == targetId && !b.IsDeleted)
                    .Select(b => new { b.Year, b.Month })
                    .ToListAsync();
                var targetKeySet = targetBudgetKeys.Select(k => (k.Year, k.Month)).ToHashSet();

                foreach (var budget in sourceBudgets)
                {
                    // Same-month budget already exists on the target: dropping the source
                    // duplicate instead of moving it, since (CategoryId, Year, Month) is unique.
                    if (targetKeySet.Contains((budget.Year, budget.Month)))
                    {
                        budget.IsDeleted = true;
                        budget.DeletedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        budget.CategoryId = targetId;
                    }
                }

                var children = await _context.Categories
                    .Where(c => c.ParentId == sourceId)
                    .ToListAsync();
                foreach (var child in children)
                    child.ParentId = targetId;

                var source = await _context.Categories.FindAsync(sourceId);
                if (source is not null)
                    _context.Categories.Remove(source);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging category {SourceId} into {TargetId}.", sourceId, targetId);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string name, int? excludeId = null)
        {
            try
            {
                return await _context.Categories
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