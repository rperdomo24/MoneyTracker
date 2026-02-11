using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class BudgetRepository : IBudgetRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<BudgetRepository> _logger;

        public BudgetRepository(MoneyTrackerDbContext context, ILogger<BudgetRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Budget?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Budgets
                    .AsNoTracking()
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Budget by ID: {Id}", id);
                return null;
            }
        }

        public async Task<Budget?> GetByCategoryMonthAsync(int categoryId, int year, int month)
        {
            try
            {
                return await _context.Budgets
                    .FirstOrDefaultAsync(b =>
                        !b.IsDeleted &&
                        b.CategoryId == categoryId &&
                        b.Year == year &&
                        b.Month == month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Budget by Category/Month: {CategoryId} {Year}-{Month}", categoryId, year, month);
                return null;
            }
        }

        public async Task<List<Budget>> GetByMonthAsync(int year, int month, int? categoryType = null)
        {
            try
            {
                var query = _context.Budgets
                    .AsNoTracking()
                    .Include(b => b.Category)
                    .Where(b => !b.IsDeleted && b.Year == year && b.Month == month)
                    .AsQueryable();

                if (categoryType.HasValue)
                {
                    query = query.Where(b => b.Category != null && (int)b.Category.Type == categoryType.Value);
                }

                return await query
                    .OrderBy(b => b.Category.Type)
                    .ThenBy(b => b.Category.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Budgets by month: {Year}-{Month}", year, month);
                return new List<Budget>();
            }
        }

        public async Task AddAsync(Budget budget)
        {
            try
            {
                _context.Budgets.Add(budget);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Budget: CategoryId={CategoryId} {Year}-{Month}", budget.CategoryId, budget.Year, budget.Month);
                throw;
            }
        }

        public async Task UpdateAsync(Budget budget)
        {
            try
            {
                _context.Budgets.Update(budget);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Budget: Id={Id}", budget.Id);
                throw;
            }
        }

        public async Task SoftDeleteAsync(int id)
        {
            try
            {
                var entity = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
                if (entity is null) return;

                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting Budget: Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Retorna [categoryId + todos sus hijos recursivos]
        /// </summary>
        public async Task<List<int>> GetCategoryAndChildrenIdsAsync(int categoryId)
        {
            try
            {
                // Cargamos todo el árbol en memoria (para arrancar simple).
                // Si luego crece mucho, lo optimizamos con CTE.
                var categories = await _context.Categories
                    .AsNoTracking()
                    .Select(c => new { c.Id, c.ParentId })
                    .ToListAsync();

                var result = new List<int>();
                var stack = new Stack<int>();
                stack.Push(categoryId);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (result.Contains(current)) continue;

                    result.Add(current);

                    var children = categories
                        .Where(c => c.ParentId == current)
                        .Select(c => c.Id);

                    foreach (var childId in children)
                        stack.Push(childId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category children IDs for CategoryId={CategoryId}", categoryId);
                return new List<int> { categoryId };
            }
        }
    }
}
