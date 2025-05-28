using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Interfaces
{
    public interface IAccountService
    {
        Task<OperationResult<List<AccountDto>>> GetAllAsync();
        Task<OperationResult<AccountDto>> GetByIdAsync(int id);
        Task<OperationResult> CreateAsync(AccountDto dto);
        Task<OperationResult> UpdateAsync(AccountDto dto);
        Task<OperationResult> DeleteAsync(int id);
    }
}
