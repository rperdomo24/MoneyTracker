using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(IServiceScopeFactory scopeFactory, ILogger<AccountRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<List<Account>> GetAllAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                return await context.Accounts
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all accounts.");
                return new List<Account>();
            }
        }

        public async Task<Account?> GetByIdAsync(int id)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                return await context.Accounts.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching account by ID: {Id}", id);
                return null;
            }
        }

        public async Task<bool> AddAsync(Account account)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                await using var transaction = await context.Database.BeginTransactionAsync();

                context.Accounts.Add(account);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding account: {Name}", account.Name);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Account account)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                await using var transaction = await context.Database.BeginTransactionAsync();

                context.Accounts.Update(account);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account ID: {Id}", account.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                await using var transaction = await context.Database.BeginTransactionAsync();

                var acc = await context.Accounts.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                if (acc != null)
                {
                    acc.IsDeleted = true;
                    acc.DeletedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account ID: {Id}", id);
                return false;
            }
        }

        public async Task<bool> HasAccountsByTypeAsync(AccountType accountType)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                IQueryable<Account> query = context.Accounts;

                if (accountType != AccountType.None)
                {
                    query = query
                        .AsNoTracking()
                        .Where(a => !a.IsDeleted && a.Type == accountType);
                }
                else
                {
                    query = query
                        .AsNoTracking()
                        .Where(a => !a.IsDeleted);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking accounts by type: {AccountType}", accountType);
                return false;
            }
        }

        public async Task<List<Account>> GetAllCreditAccountsWithDatesAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                return await context.Accounts
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted
                        && a.Type == AccountType.Credit
                        && (a.CutDay != null || a.PaymentDay != null))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching credit accounts with dates.");
                return new List<Account>();
            }
        }
    }
}
