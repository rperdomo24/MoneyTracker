using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;
using Npgsql;
using NpgsqlTypes;

public class TransactionRepository : ITransactionRepository
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionRepository> _logger;
    private readonly ITenantContext _tenantContext;

    public TransactionRepository(
        IServiceScopeFactory scopeFactory,
        ILogger<TransactionRepository> logger,
        ITenantContext tenantContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<List<Transaction>> GetAllAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            return await context.Transaction
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            return await context.Transaction
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            context.Transaction.Add(transaction);
            await context.SaveChangesAsync();
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            context.Transaction.Update(transaction);
            await context.SaveChangesAsync();
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            var entity = await context.Transaction.FindAsync(id);
            if (entity is not null)
            {
                context.Transaction.Remove(entity);
                await context.SaveChangesAsync();
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            _ = transactionTypeIds;

            var query = context.Transaction
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            var query = context.Transaction
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

    public async Task<List<DashboardTransactionEntry>> GetDashboardEntriesAsync(
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        List<int> accountIds)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            var query = context.Transaction
                .AsNoTracking()
                .Where(t => !t.IsDeleted);

            query = ApplyDateFilter(query, fromDateUtc, toDateUtc);

            if (accountIds.Any())
            {
                query = query.Where(t => accountIds.Contains(t.AccountId));
            }

            return await query
                .Select(t => new DashboardTransactionEntry
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description ?? string.Empty,
                    Amount = t.Amount,
                    Date = t.Date,
                    AccountId = t.AccountId,
                    AccountName = t.Account != null ? t.Account.Name : "Cuenta desconocida",
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category != null ? t.Category.Name : "Sin categoría",
                    CategoryColor = t.Category != null && t.Category.Color != null ? t.Category.Color : "#9e9e9e",
                    CategoryType = t.Category != null ? t.Category.Type : CategoryTypeEnum.Transfer,
                    SystemCategoryCode = t.Category != null ? t.Category.SystemCategoryCode : null
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard transaction entries");
            return new List<DashboardTransactionEntry>();
        }
    }

    public async Task<List<DashboardTransactionEntry>> GetRecentDashboardEntriesAsync(
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        List<int> accountIds,
        CategoryTypeEnum? categoryType,
        bool includeTransfers,
        int count)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            var safeCount = Math.Max(1, count);
            var query = context.Transaction
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
            else if (!includeTransfers)
            {
                query = query.Where(t =>
                    t.Category != null &&
                    (t.Category.Type == CategoryTypeEnum.Income || t.Category.Type == CategoryTypeEnum.Expense));
            }

            return await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Take(safeCount)
                .Select(t => new DashboardTransactionEntry
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description ?? string.Empty,
                    Amount = t.Amount,
                    Date = t.Date,
                    AccountId = t.AccountId,
                    AccountName = t.Account != null ? t.Account.Name : "Cuenta desconocida",
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category != null ? t.Category.Name : "Sin categoría",
                    CategoryColor = t.Category != null && t.Category.Color != null ? t.Category.Color : "#9e9e9e",
                    CategoryType = t.Category != null ? t.Category.Type : CategoryTypeEnum.Transfer,
                    SystemCategoryCode = t.Category != null ? t.Category.SystemCategoryCode : null
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent dashboard transaction entries");
            return new List<DashboardTransactionEntry>();
        }
    }

    public async Task<List<DashboardCategoryAggregateEntry>> GetCategoryAggregatesAsync(
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        List<int> accountIds,
        CategoryTypeEnum categoryType,
        int top = 0)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            var query = context.Transaction
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.Category != null && t.Category.Type == categoryType);

            query = ApplyDateFilter(query, fromDateUtc, toDateUtc);

            if (accountIds.Any())
            {
                query = query.Where(t => accountIds.Contains(t.AccountId));
            }

            IQueryable<DashboardCategoryAggregateEntry> grouped = query
                .GroupBy(t => new
                {
                    t.CategoryId,
                    CategoryName = t.Category != null ? t.Category.Name : "Sin categoría",
                    CategoryColor = t.Category != null && t.Category.Color != null ? t.Category.Color : "#9e9e9e"
                })
                .Select(g => new DashboardCategoryAggregateEntry
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    CategoryColor = g.Key.CategoryColor,
                    Amount = g.Sum(x => Math.Abs(x.Amount)),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(x => x.Amount);

            if (top > 0)
            {
                grouped = grouped.Take(top);
            }

            return await grouped.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard category aggregates");
            return new List<DashboardCategoryAggregateEntry>();
        }
    }

    public async Task<List<DashboardAmountByDateEntry>> GetAmountsByDateAsync(
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        List<int> accountIds,
        CategoryTypeEnum categoryType)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            var query = context.Transaction
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.Category != null && t.Category.Type == categoryType);

            query = ApplyDateFilter(query, fromDateUtc, toDateUtc);

            if (accountIds.Any())
            {
                query = query.Where(t => accountIds.Contains(t.AccountId));
            }

            return await query
                .GroupBy(t => t.Date.Date)
                .Select(g => new DashboardAmountByDateEntry
                {
                    Date = g.Key,
                    Amount = g.Sum(x => Math.Abs(x.Amount))
                })
                .OrderBy(x => x.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard amounts by date");
            return new List<DashboardAmountByDateEntry>();
        }
    }

    public async Task<List<DashboardCashFlowAggregateEntry>> GetCashFlowAggregatesAsync(
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        List<int> accountIds)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;
            if (!tenantId.HasValue || tenantId == Guid.Empty)
            {
                _logger.LogWarning("Tenant context not found while loading cash flow aggregates.");
                return new List<DashboardCashFlowAggregateEntry>();
            }

            const string sql = """
                SELECT
                    c."Type" AS CategoryType,
                    t."CategoryId" AS CategoryId,
                    COALESCE(c."Name", 'Uncategorized') AS CategoryName,
                    SUM(ABS(t."Amount")) AS Amount,
                    COUNT(*)::int AS TransactionCount
                FROM "Transaction" t
                INNER JOIN "Categories" c ON c."Id" = t."CategoryId"
                WHERE
                    t."TenantId" = @tenantId
                    AND c."TenantId" = @tenantId
                    AND t."IsDeleted" = FALSE
                    AND c."IsDeleted" = FALSE
                    AND (@fromUtc IS NULL OR t."Date" >= @fromUtc)
                    AND (@toUtc IS NULL OR t."Date" <= @toUtc)
                    AND c."Type" IN (1, 2)
                    AND (
                        @hasAccounts = FALSE
                        OR t."AccountId" = ANY(@accountIds)
                    )
                GROUP BY c."Type", t."CategoryId", c."Name"
                ORDER BY Amount DESC;
                """;

            var result = new List<DashboardCashFlowAggregateEntry>();
            var safeAccountIds = accountIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();
            return await ExecuteCashFlowAggregateSqlAsync(
                sql,
                tenantId.Value,
                fromDateUtc,
                toDateUtc,
                safeAccountIds,
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cash flow aggregates");
            return new List<DashboardCashFlowAggregateEntry>();
        }
    }

    private async Task<List<DashboardCashFlowAggregateEntry>> ExecuteCashFlowAggregateSqlAsync(
        string sql,
        Guid tenantId,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int[] safeAccountIds,
        List<DashboardCashFlowAggregateEntry> result)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

        var connectionString = context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError("Database connection string is not configured.");
            return result;
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("tenantId", tenantId));
        command.Parameters.Add(new NpgsqlParameter("fromUtc", fromDateUtc.HasValue ? fromDateUtc.Value : DBNull.Value));
        command.Parameters.Add(new NpgsqlParameter("toUtc", toDateUtc.HasValue ? toDateUtc.Value : DBNull.Value));
        command.Parameters.Add(new NpgsqlParameter("hasAccounts", safeAccountIds.Length > 0));
        command.Parameters.Add(new NpgsqlParameter<int[]>("accountIds", safeAccountIds)
        {
            NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer
        });

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new DashboardCashFlowAggregateEntry
            {
                CategoryType = (CategoryTypeEnum)reader.GetInt32(0),
                CategoryId = reader.GetInt32(1),
                CategoryName = reader.GetString(2),
                Amount = reader.GetDecimal(3),
                TransactionCount = reader.GetInt32(4)
            });
        }

        return result;
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            context.Transaction.Add(transaction);
            await context.SaveChangesAsync();
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
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

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

        return await context.Transaction
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            return await context.Transaction
                .Where(t => t.AccountId == accountId && !t.IsDeleted)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating balance for account {AccountId}", accountId);
            return 0m;
        }
    }

    public async Task<List<int>> SoftDeleteByAccountAsync(int accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            var nowUtc = DateTime.UtcNow;

            var rootTransactions = await context.Transaction
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.AccountId == accountId)
                .Select(t => new { t.Id, t.TransferPairId, t.AccountId })
                .ToListAsync(cancellationToken);

            if (rootTransactions.Count == 0)
            {
                return new List<int>();
            }

            var transactionIds = rootTransactions
                .Select(t => t.Id)
                .ToHashSet();

            var affectedAccountIds = rootTransactions
                .Select(t => t.AccountId)
                .ToHashSet();

            var pairedIds = rootTransactions
                .Where(t => t.TransferPairId.HasValue)
                .Select(t => t.TransferPairId!.Value)
                .ToList();

            if (pairedIds.Count > 0)
            {
                var pairedTransactions = await context.Transaction
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted && pairedIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.AccountId })
                    .ToListAsync(cancellationToken);

                foreach (var paired in pairedTransactions)
                {
                    transactionIds.Add(paired.Id);
                    affectedAccountIds.Add(paired.AccountId);
                }
            }

            var transactionsToDelete = await context.Transaction
                .Where(t => !t.IsDeleted && transactionIds.Contains(t.Id))
                .Include(t => t.Attachments)
                .ToListAsync(cancellationToken);

            foreach (var transaction in transactionsToDelete)
            {
                transaction.IsDeleted = true;
                transaction.DeletedAt = nowUtc;
                transaction.UpdatedAt = nowUtc;

                foreach (var attachment in transaction.Attachments.Where(a => !a.IsDeleted))
                {
                    attachment.IsDeleted = true;
                    attachment.DeletedAt = nowUtc;
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            return affectedAccountIds.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting transactions by account {AccountId}", accountId);
            throw;
        }
    }
}
