using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;
using System;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<TransactionRepository> _logger;

        public TransactionRepository(MoneyTrackerDbContext context, ILogger<TransactionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Transaction>> GetAllAsync()
        {
            return await _context.Transaction
                .Include(e => e.Category)
                .Include(e => e.Account)
                .ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(int id)
        {
            return await _context.Transaction
                .Include(e => e.Category)
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task AddAsync(Transaction expense)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Transaction.Add(expense);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding expense with Name: {Name}", expense.Name);
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(Transaction expense)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Transaction.Update(expense);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense with ID: {Id}", expense.Id);
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.Transaction.FindAsync(id);
                if (entity is not null)
                {
                    _context.Transaction.Remove(entity);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense with ID: {Id}", id);
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}