using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Interfaces
{
    public interface IIncomeService
    {
        Task<OperationResult<List<IncomeDto>>> GetAllAsync();
        Task<OperationResult<IncomeDto>> GetByIdAsync(int id);
        Task<OperationResult> AddAsync(IncomeDto dto);
        Task<OperationResult> UpdateAsync(IncomeDto dto);
        Task<OperationResult> DeleteAsync(int id);
    }
}
