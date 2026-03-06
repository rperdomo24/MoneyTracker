using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.Common.Extensions
{
    public static class TransactionDtoExtensions
    {
        public static bool IsIncome(this TransactionDto transaction)
            => !transaction.IsTransfer()
               && transaction.Category?.Type == CategoryTypeEnum.Income;

        public static bool IsExpense(this TransactionDto transaction)
            => !transaction.IsTransfer()
               && transaction.Category?.Type == CategoryTypeEnum.Expense;

        public static bool IsCreditPayment(this TransactionDto transaction)
            => SystemCategoryCodes.IsCreditRelated(transaction.Category?.SystemCategoryCode);

        public static bool IsTransfer(this TransactionDto transaction)
            => SystemCategoryCodes.IsTransfer(transaction.Category?.SystemCategoryCode)
               || transaction.Category?.Type == CategoryTypeEnum.Transfer;

        public static TransactionTypeEnum GetDerivedType(this TransactionDto transaction)
        {
            if (transaction.IsTransfer())
                return TransactionTypeEnum.Transfer;

            return transaction.Category?.Type == CategoryTypeEnum.Income
                ? TransactionTypeEnum.Income
                : TransactionTypeEnum.Expense;
        }

        public static bool IsOutgoingTransfer(this TransactionDto transaction)
            => SystemCategoryCodes.IsTransferOut(transaction.Category?.SystemCategoryCode);

        public static bool IsIncomingTransfer(this TransactionDto transaction)
            => SystemCategoryCodes.IsTransferIn(transaction.Category?.SystemCategoryCode);

        public static TransactionDto GetFromTransaction(this TransactionDto transaction, TransactionDto pairedTransaction)
            => transaction.IsOutgoingTransfer() ? transaction : pairedTransaction;

        public static TransactionDto GetToTransaction(this TransactionDto transaction, TransactionDto pairedTransaction)
            => transaction.IsOutgoingTransfer() ? pairedTransaction : transaction;

        public static string GetAmountDisplay(this TransactionDto transaction)
        {
            if (transaction.Amount == 0) return "$0.00";
            return transaction.Amount.ToString("C");
        }

        public static string GetAmountDisplayWithSign(this TransactionDto transaction)
        {
            if (transaction.Amount == 0) return "$0.00";
            return transaction.IsTransfer()
                ? FormatWithSign(transaction.Amount)
                : transaction.Amount.ToString("C");
        }

        private static string FormatWithSign(decimal amount)
        {
            if (amount == 0) return "$0.00";
            return amount >= 0 ? $"+${amount:N2}" : $"-${Math.Abs(amount):N2}";
        }

        public static decimal GetSignedAmount(this TransactionDto transaction)
        {
            if (transaction.IsTransfer())
            {
                return transaction.IsIncomingTransfer()
                    ? Math.Abs(transaction.Amount)
                    : Math.Abs(transaction.Amount) * -1;
            }

            return transaction.IsIncome()
                ? Math.Abs(transaction.Amount)
                : Math.Abs(transaction.Amount) * -1;
        }

        public static decimal GetAbsoluteAmountss(this TransactionDto transaction)
            => Math.Abs(transaction.Amount);
    }
}
