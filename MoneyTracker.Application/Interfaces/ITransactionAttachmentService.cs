using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.Application.Interfaces;

public interface ITransactionAttachmentService
{
    Task<OperationResult<List<TransactionAttachmentDto>>> GetByTransactionIdAsync(int transactionId);
    Task<OperationResult<TransactionAttachmentDto>> AddAsync(int transactionId, string fileName, string? contentType, byte[] content);
    Task<OperationResult> DeleteAsync(int attachmentId);
}
