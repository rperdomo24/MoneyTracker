using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;
using System;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<ExpenseRepository> _logger;

        public ExpenseRepository(MoneyTrackerDbContext context, ILogger<ExpenseRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Expense>> GetAllAsync()
        {
            return await _context.Expenses
                .Include(e => e.Category)
                .Include(e => e.Account)
                .ToListAsync();
        }

        public async Task<Expense?> GetByIdAsync(int id)
        {
            return await _context.Expenses
                .Include(e => e.Category)
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task AddAsync(Expense expense)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Expenses.Add(expense);
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

        public async Task UpdateAsync(Expense expense)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Expenses.Update(expense);
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
                var entity = await _context.Expenses.FindAsync(id);
                if (entity is not null)
                {
                    _context.Expenses.Remove(entity);
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