using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Account;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetAllAsync();
        Task<Account?> GetByIdAsync(int id);
        Task<bool> AddAsync(Account account);
        Task<bool> UpdateAsync(Account account);
        Task<bool> DeleteAsync(int id);
        Task<bool> HasAccountsByTypeAsync(AccountType accountType);
    }
}
