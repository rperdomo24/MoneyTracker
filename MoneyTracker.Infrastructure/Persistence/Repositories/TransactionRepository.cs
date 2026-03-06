using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

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

    public async Task<List<Transaction>> GetFilteredAsync(
        DateTime? fromDate,
        DateTime? toDate,
        List<int> accountIds,
        List<int> transactionTypeIds,
        bool skipSorting = false)
    {
        try
        {
            _ = transactionTypeIds;

            var query = _context.Transaction
                .Where(t => !t.IsDeleted)
                .AsNoTracking()
                .Include(e => e.Category)
                .Include(e => e.Account)
                .AsQueryable();

            query = ApplyDateFilter(query, fromDate, toDate);

            if (accountIds.Any())
            {
                query = query.Where(t => accountIds.Contains(t.AccountId));
            }

            if (!skipSorting)
            {
                query = query
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.CreatedAt);
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered transactions");
            return new List<Transaction>();
        }
    }

    public async Task<List<TransactionTrendEntry>> GetTrendEntriesAsync(
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        List<int> accountIds,
        CategoryTypeEnum? categoryType = null)
    {
        try
        {
            var query = _context.Transaction
                .AsNoTracking()
                .Where(t => !t.IsDeleted);

            query = ApplyDateFilter(query, fromDateUtc, toDateUtc);

            if (accountIds.Any())
            {
                query = query.Where(t => accountIds.Contains(t.AccountId));
            }

            if (categoryType.HasValue)
            {
                query = query.Where(t => t.Category != null && t.Category.Type == categoryType.Value);
            }
            else
            {
                query = query.Where(t =>
                    t.Category != null &&
                    (t.Category.Type == CategoryTypeEnum.Income || t.Category.Type == CategoryTypeEnum.Expense));
            }

            return await query
                .Select(t => new TransactionTrendEntry
                {
                    Date = t.Date,
                    Amount = t.Amount,
                    CategoryType = t.Category != null ? t.Category.Type : CategoryTypeEnum.Transfer
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trend transaction entries");
            return new List<TransactionTrendEntry>();
        }
    }

    private IQueryable<Transaction> ApplyDateFilter(IQueryable<Transaction> query, DateTime? startDateUtc, DateTime? endDateUtc)
    {
        if (startDateUtc.HasValue)
            query = query.Where(t => t.Date >= startDateUtc.Value);

        if (endDateUtc.HasValue)
            query = query.Where(t => t.Date <= endDateUtc.Value);

        return query;
    }

    public async Task<int> AddAndReturnIdAsync(Transaction transaction)
    {
        try
        {
            _context.Transaction.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transaction and returning ID");
            throw;
        }
    }

    public async Task<List<Transaction>> GetByCategoryTreeAsync(
        int categoryId,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var sql = @"
        WITH RECURSIVE category_tree AS (
            SELECT ""Id""
            FROM ""Categories""
            WHERE ""Id"" = {0}

            UNION ALL

            SELECT c.""Id""
            FROM ""Categories"" c
            INNER JOIN category_tree ct
                ON c.""ParentId"" = ct.""Id""
        )
        SELECT t.*
        FROM ""Transaction"" t
        WHERE t.""CategoryId"" IN (SELECT ""Id"" FROM category_tree)
          AND t.""Date"" BETWEEN {1} AND {2}
          AND NOT t.""IsDeleted""
        ORDER BY t.""Date"" DESC";

        return await _context.Transaction
            .FromSqlRaw(sql, categoryId, fromUtc, toUtc)
            .Include(t => t.Category)
            .Include(t => t.Account)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<decimal> GetAccountBalanceAsync(int accountId)
    {
        try
        {
            return await _context.Transaction
                .Where(t => t.AccountId == accountId && !t.IsDeleted)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating balance for account {AccountId}", accountId);
            return 0m;
        }
    }
}
