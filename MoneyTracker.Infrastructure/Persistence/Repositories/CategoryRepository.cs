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
                    .Include(c => c.Subcategories)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories.");
                return new();
            }
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Categories
                    .Include(c => c.Subcategories)
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding category.");
            }
        }

        public async Task UpdateAsync(Category category)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating category.");
            }
        }

        public async Task DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.Categories.FindAsync(id);
                if (entity is null)
                    return;

                _context.Categories.Remove(entity);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting category with ID {Id}.", id);
            }
        }

        public async Task<bool> ExistsAsync(string name, int? excludeId = null)
        {
            try
            {
                return await _context.Categories
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