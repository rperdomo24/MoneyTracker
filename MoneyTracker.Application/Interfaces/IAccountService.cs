using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Enums;

namespace MoneyTracker.Application.Interfaces
{
    public interface IAccountService
    {
        Task<OperationResult<List<AccountDto>>> GetAllAsync();
        Task<OperationResult<AccountDto>> GetByIdAsync(int id);
        Task<OperationResult> UpdateAsync(AccountDto dto);
        Task<OperationResult> DeleteAsync(int id);
        Task<OperationResult> CreateAsync(AccountDto dto);
        Task<OperationResult> CreateWithInitialBalanceAsync(AccountDto dto, decimal initialBalance);
        Task<OperationResult> AdjustBalanceAsync(int accountId, decimal newBalance, string reason = "");
        Task<OperationResult<decimal>> GetCurrentBalanceAsync(int accountId);

        Task<OperationResult<List<AccountDto>>> GetAccountsWithBalancesAsync();
        Task<OperationResult<bool>> HasAccountByType(AccountType accountType);
    }
}
