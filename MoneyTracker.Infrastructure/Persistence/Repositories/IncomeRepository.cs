using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class IncomeRepository : IIncomeRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<IncomeRepository> _logger;

        public IncomeRepository(MoneyTrackerDbContext context, ILogger<IncomeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Income>> GetAllAsync()
        {
            return await _context.Incomes
                .Include(i => i.Category)
                .Include(i => i.Account)
                .ToListAsync();
        }

        public async Task<Income?> GetByIdAsync(int id)
        {
            return await _context.Incomes
                .Include(i => i.Category)
                .Include(i => i.Account)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> AddAsync(Income income)
        {
            try
            {
                _context.Incomes.Add(income);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding income: {@Income}", income);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Income income)
        {
            try
            {
                _context.Incomes.Update(income);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating income: {@Income}", income);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var entity = await _context.Incomes.FindAsync(id);
                if (entity is null) return false;

                _context.Incomes.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting income with Id: {Id}", id);
                return false;
            }
        }
    }

}
