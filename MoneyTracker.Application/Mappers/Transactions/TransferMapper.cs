using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
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
            TransferTypeEnum transferType,
            string fromAccountName,
            string toAccountName,
            ITimeZoneService timeZoneService)
        {
            var dateUtc = timeZoneService.ConvertToUtc(dto.Date);
            var nowUtc = timeZoneService.GetNowInUtc();

            var description = string.IsNullOrWhiteSpace(dto.Description)
                ? $"Transfer from {fromAccountName} to {toAccountName}"
                : dto.Description;

            var fromTransaction = new Transaction
            {
                Name = $"Transfer to {toAccountName}",
                Amount = dto.Amount,
                Date = dateUtc,
                Description = description,
                AccountId = dto.FromAccountId,
                CategoryId = fromCategoryId,
                TransferType = transferType,
                IsSystemGenerated = false,
                Status = TransactionStatus.Completed,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };

            var toTransaction = new Transaction
            {
                Name = $"Transfer from {fromAccountName}",
                Amount = dto.Amount,
                Date = dateUtc,
                Description = description,
                AccountId = dto.ToAccountId,
                CategoryId = toCategoryId,
                TransferType = transferType,
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

            transaction.Amount = dto.Amount;
            transaction.Date = dateUtc;
            transaction.Description = dto.Description;
            transaction.UpdatedAt = nowUtc;

            paired.Amount = dto.Amount;
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
                TransferType = transaction.TransferType
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
                Description = string.IsNullOrWhiteSpace(fromTransaction.Description)
                    ? null
                    : $"{fromTransaction.Description} (Copy)"
            };
        }

    }
}
