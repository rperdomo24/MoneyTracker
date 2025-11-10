using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.Validators.Transaction
{
    public class TransactionWithPairDto
    {
        public TransactionDto Transaction { get; set; } = default!;
        public TransactionDto? PairedTransaction { get; set; }
        public bool IsTransfer => PairedTransaction != null;
        public TransferTypeEnum? TransferType { get; set; }
    }
}
