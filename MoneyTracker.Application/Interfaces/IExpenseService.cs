using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Interfaces
{
    public interface IExpenseService
    {
        Task<OperationResult<List<ExpenseDto>>> GetAllAsync();
        Task<OperationResult<ExpenseDto>> GetByIdAsync(int id);
        Task<OperationResult<bool>> CreateAsync(ExpenseDto dto);
        Task<OperationResult<bool>> UpdateAsync(ExpenseDto dto);
        Task<OperationResult<bool>> DeleteAsync(int id);
    }
}
