using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.Mappers.Transactions
{
    public static class TransferMapper
    {
        public static (Transaction from, Transaction to) MapToTransferPair(
            this CreateTransferDto dto,
            int fromCategoryId,
            int toCategoryId,
            string transferTypeName,
            string fromAccountName,
            string toAccountName,
            ITimeZoneService timeZoneService)
        {
            var dateUtc = timeZoneService.ConvertToUtc(dto.Date);
            var nowUtc = timeZoneService.GetNowInUtc();

            var description = string.IsNullOrWhiteSpace(dto.Description)
                ? $"{transferTypeName} from {fromAccountName} to {toAccountName}"
                : dto.Description;

            var fromTransaction = new Transaction
            {
                Name = $"{transferTypeName} to {toAccountName}",
                Amount = dto.Amount * -1,
                Date = dateUtc,
                Description = description,
                AccountId = dto.FromAccountId,
                CategoryId = fromCategoryId,
                PaymentMethod = dto.PaymentMethod,
                IsSystemGenerated = false,
                Status = TransactionStatus.Completed,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };

            var toTransaction = new Transaction
            {
                Name = $"{transferTypeName} from {fromAccountName}",
                Amount = dto.Amount,
                Date = dateUtc,
                Description = description,
                AccountId = dto.ToAccountId,
                CategoryId = toCategoryId,
                PaymentMethod = dto.PaymentMethod,
                IsSystemGenerated = false,
                Status = TransactionStatus.Completed,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };

            return (fromTransaction, toTransaction);
        }

        public static void UpdateTransferPair(
            Transaction transaction,
            Transaction paired,
            UpdateTransferDto dto,
            ITimeZoneService timeZoneService)
        {
            var dateUtc = timeZoneService.ConvertToUtc(dto.Date);
            var nowUtc = timeZoneService.GetNowInUtc();

            var transactionCode = transaction.Category?.SystemCategoryCode;
            var pairedCode = paired.Category?.SystemCategoryCode;

            transaction.Amount = SystemCategoryCodes.IsTransferIn(transactionCode) ? Math.Abs(dto.Amount) : dto.Amount * -1;
            transaction.Date = dateUtc;
            transaction.Description = dto.Description;
            transaction.UpdatedAt = nowUtc;

            paired.Amount = SystemCategoryCodes.IsTransferIn(pairedCode) ? Math.Abs(dto.Amount) : dto.Amount * -1;
            paired.Date = dateUtc;
            paired.Description = dto.Description;
            paired.UpdatedAt = nowUtc;
        }

        public static TransactionWithPairDto MapToTransactionWithPair(
            this Transaction transaction,
            Transaction? paired,
            ITimeZoneService timeZoneService)
        {
            return new TransactionWithPairDto
            {
                Transaction = transaction.MapToDto(timeZoneService),
                PairedTransaction = paired?.MapToDto(timeZoneService),
            };
        }

        public static CreateTransferDto MapToDuplicateTransferDto(
            Transaction fromTransaction,
            Transaction toTransaction,
            ITimeZoneService timeZoneService)
        {
            return new CreateTransferDto
            {
                FromAccountId = fromTransaction.AccountId,
                ToAccountId = toTransaction.AccountId,
                Amount = fromTransaction.Amount,
                Date = DateTime.Today,
                PaymentMethod = fromTransaction.PaymentMethod,
                Description = string.IsNullOrWhiteSpace(fromTransaction.Description)
                    ? null
                    : $"{fromTransaction.Description} (Copy)"
            };
        }
    }
}
