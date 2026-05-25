using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.RecurringTransactions;

namespace MoneyTracker.Application.Interfaces
{
    public interface IRecurringTransactionService
    {
        Task<OperationResult<List<RecurringTransactionDto>>> GetAllAsync();
        Task<OperationResult<RecurringTransactionDto>> GetByIdAsync(int id);
        Task<OperationResult> CreateAsync(RecurringTransactionDto dto);
        Task<OperationResult> UpdateAsync(RecurringTransactionDto dto);
        Task<OperationResult> DeleteAsync(int id);
        Task<OperationResult> ToggleActiveAsync(int id);
    }
}
