using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<OperationResult<List<TransactionDto>>> GetAllAsync();
        Task<OperationResult<TransactionDto>> GetByIdAsync(int id);
        Task<OperationResult<bool>> CreateAsync(TransactionDto dto);
        Task<OperationResult<bool>> UpdateAsync(TransactionDto dto);
        Task<OperationResult<bool>> DeleteAsync(int id);
    }
}
