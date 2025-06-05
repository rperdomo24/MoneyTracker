using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;
using MoneyTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        try
        {
            return await _context.Transaction
                .Include(e => e.Category)
                .Include(e => e.Account)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all transactions");
            return new List<Transaction>();
        }
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Transaction
                .Include(e => e.Category)
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e => e.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction by ID: {Id}", id);
            return null;
        }
    }

    public async Task AddAsync(Transaction transaction)
    {
        try
        {
            _context.Transaction.Add(transaction);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transaction with Name: {Name}", transaction.Name);
            throw;
        }
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        try
        {
            _context.Transaction.Update(transaction);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction with ID: {Id}", transaction.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var entity = await _context.Transaction.FindAsync(id);
            if (entity is not null)
            {
                _context.Transaction.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction with ID: {Id}", id);
            throw;
        }
    }
}