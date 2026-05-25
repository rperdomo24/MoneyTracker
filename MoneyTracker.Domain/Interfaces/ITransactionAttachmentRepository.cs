using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces;

public interface ITransactionAttachmentRepository
{
    Task<List<TransactionAttachment>> GetByTransactionIdAsync(int transactionId);
    Task<TransactionAttachment?> GetByIdAsync(int id);
    Task<TransactionAttachment> AddAsync(TransactionAttachment attachment);
    Task DeleteAsync(TransactionAttachment attachment);
}
