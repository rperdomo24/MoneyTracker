using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class MerchantRepository : IMerchantRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<MerchantRepository> _logger;

        public MerchantRepository(MoneyTrackerDbContext context, ILogger<MerchantRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Merchant>> GetAllAsync()
        {
            try
            {
                return await _context.Merchants
                    .AsNoTracking()
                    .Where(m => !m.IsDeleted)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all merchants.");
                return new();
            }
        }

        public async Task<Merchant?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Merchants
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting merchant by ID {Id}.", id);
                return null;
            }
        }

        public async Task AddAsync(Merchant merchant)
        {
            try
            {
                _context.Merchants.Add(merchant);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding merchant.");
                throw;
            }
        }

        public async Task UpdateAsync(Merchant merchant)
        {
            try
            {
                _context.Merchants.Update(merchant);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merchant.");
                throw;
            }
        }

        public async Task SoftDeleteAsync(int id)
        {
            try
            {
                var entity = await _context.Merchants.FindAsync(id);
                if (entity is null) return;

                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting merchant with ID {Id}.", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string name, int? excludeId = null)
        {
            try
            {
                return await _context.Merchants
                    .AsNoTracking()
                    .AnyAsync(m => !m.IsDeleted
                        && m.Name == name
                        && (!excludeId.HasValue || m.Id != excludeId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking merchant existence.");
                return false;
            }
        }
    }
}
