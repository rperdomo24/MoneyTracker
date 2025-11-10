using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Validators.Transaction;

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
        Task<OperationResult<bool>> CreateTransferAsync(CreateTransferDto dto);

        Task<OperationResult<TransactionWithPairDto>> GetByIdWithPairAsync(int id);
        Task<OperationResult<bool>> UpdateTransferAsync(UpdateTransferDto dto);
        Task<OperationResult<bool>> DeleteTransferAsync(int transactionId);
        Task<OperationResult<int>> DuplicateTransactionAsync(int transactionId);
    }
}
