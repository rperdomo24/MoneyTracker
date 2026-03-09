using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(MoneyTrackerDbContext context, ILogger<AccountRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Account>> GetAllAsync()
        {
            try
            {
                return await _context.Accounts
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
                return await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching account by ID: {Id}", id);
                return null;
            }
        }

        public async Task<bool> AddAsync(Account account)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding account: {Name}", account.Name);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Account account)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Accounts.Update(account);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating account ID: {Id}", account.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var acc = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                if (acc != null)
                {
                    acc.IsDeleted = true;
                    acc.DeletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting account ID: {Id}", id);
                return false;
            }
        }

        public async Task<bool> HasAccountsByTypeAsync(AccountType accountType)
        {
            try
            {
                IQueryable<Account> query = _context.Accounts;

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
    }
}
