using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Filters;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;
using System.Linq;

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

    public async Task<List<Transaction>> GetFilteredAsync(TimePeriodFilter TimePeriod, DateTime? FromDate, DateTime? ToDate, List<int> AccountIds, List<int> TransactionTypeIds)
    {
        try
        {
            var query = _context.Transaction
                .AsNoTracking()
                .Include(e => e.Category)
                .Include(e => e.Account)
                .AsQueryable();

            // Filtro por período de tiempo - las fechas ya vienen en UTC desde el mapper
            if (TimePeriod != TimePeriodFilter.Custom)
            {
                // Para períodos predefinidos, calcular fechas en UTC directamente
                var (startDateUtc, endDateUtc) = GetDateRangeUtc(TimePeriod);
                if (startDateUtc.HasValue)
                    query = query.Where(t => t.Date >= startDateUtc.Value);
                if (endDateUtc.HasValue)
                    query = query.Where(t => t.Date <= endDateUtc.Value);
            }
            else
            {
                // Para Custom, usar las fechas que ya vienen convertidas a UTC
                // Verificar y asegurar que tengan DateTimeKind.Utc
                if (FromDate.HasValue)
                {
                    var fromDateUtc = FromDate.Value.Kind == DateTimeKind.Utc ?
                        FromDate.Value :
                        DateTime.SpecifyKind(FromDate.Value, DateTimeKind.Utc);
                    query = query.Where(t => t.Date >= fromDateUtc);
                }
                if (ToDate.HasValue)
                {
                    var toDateUtc = ToDate.Value.Kind == DateTimeKind.Utc ?
                        ToDate.Value :
                        DateTime.SpecifyKind(ToDate.Value, DateTimeKind.Utc);
                    query = query.Where(t => t.Date <= toDateUtc);
                }
            }

            // Filtro por cuentas activas
            if (AccountIds.Any())
            {
                query = query.Where(t => AccountIds.Contains(t.AccountId.Value));
            }
            else
            {
                // Solo mostrar transacciones de cuentas activas por defecto
                //query = query.Where(t => t.Account.IsActive);
            }

            // Filtro por tipos de transacción (si existe en tu modelo)
            if (TransactionTypeIds.Any())
            {
                query = query.Where(t => TransactionTypeIds.Contains(t.Type.GetHashCode()));
            }

            return await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered transactions");
            return new List<Transaction>();
        }
    }

    private (DateTime? startDateUtc, DateTime? endDateUtc) GetDateRangeUtc(TimePeriodFilter timePeriod)
    {
        // Trabajar con UTC desde el inicio para evitar problemas con PostgreSQL
        var nowUtc = DateTime.UtcNow;

        return timePeriod switch
        {
            TimePeriodFilter.LastMonth => (
                DateTime.SpecifyKind(new DateTime(nowUtc.AddMonths(-1).Year, nowUtc.AddMonths(-1).Month, 1, 0, 0, 0), DateTimeKind.Utc),
                DateTime.SpecifyKind(new DateTime(nowUtc.AddMonths(-1).Year, nowUtc.AddMonths(-1).Month, DateTime.DaysInMonth(nowUtc.AddMonths(-1).Year, nowUtc.AddMonths(-1).Month), 23, 59, 59).AddMilliseconds(999), DateTimeKind.Utc)
            ),
            TimePeriodFilter.ThisMonth => (
                DateTime.SpecifyKind(new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0), DateTimeKind.Utc),
                DateTime.SpecifyKind(new DateTime(nowUtc.Year, nowUtc.Month, DateTime.DaysInMonth(nowUtc.Year, nowUtc.Month), 23, 59, 59).AddMilliseconds(999), DateTimeKind.Utc)
            ),
            TimePeriodFilter.ThisYear => (
                DateTime.SpecifyKind(new DateTime(nowUtc.Year, 1, 1, 0, 0, 0), DateTimeKind.Utc),
                DateTime.SpecifyKind(new DateTime(nowUtc.Year, 12, 31, 23, 59, 59).AddMilliseconds(999), DateTimeKind.Utc)
            ),
            _ => (null, null)
        };
    }
}

