using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<OperationResult<List<TransactionDto>>> GetAllAsync();
        Task<OperationResult<TransactionDto>> GetByIdAsync(int id);
        Task<OperationResult<bool>> CreateAsync(TransactionDto dto);
        Task<OperationResult<bool>> UpdateAsync(TransactionDto dto);
        Task<OperationResult<bool>> DeleteAsync(int id);
        Task<OperationResult<CategoryStatsDto>> GetCategoryStatsAsync(int categoryId);

        Task<OperationResult<TransactionSummaryDto>> GetFilteredAsync(TransactionFilterDto filter);
    }
}
