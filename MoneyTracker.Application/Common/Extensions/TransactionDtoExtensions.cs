using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.Common.Extensions
{
    public static class TransactionDtoExtensions
    {
        public static bool IsIncome(this TransactionDto transaction)
        {
            return transaction.Category?.Type == CategoryTypeEnum.Income;
        }

        public static bool IsExpense(this TransactionDto transaction)
        {
            return transaction.Category?.Type == CategoryTypeEnum.Expense;
        }

        public static bool IsCreditPayment(this TransactionDto transaction)
        {
            return SystemCategories.IsCreditRelatedCategory(transaction.CategoryId);
        }

        public static bool IsTransfer(this TransactionDto transaction)
        {
            return SystemCategories.IsTransferCategory(transaction.CategoryId);
        }

        public static TransactionTypeEnum GetDerivedType(this TransactionDto transaction)
        {
            // Transfers = categorías del sistema 8-13
            if (SystemCategories.IsTransferCategory(transaction.CategoryId))
                return TransactionTypeEnum.Transfer;

            // Income/Expense se deriva de la categoría
            return transaction.Category.Type == CategoryTypeEnum.Income
                ? TransactionTypeEnum.Income
                : TransactionTypeEnum.Expense;
        }

        public static bool IsOutgoingTransfer(this TransactionDto transaction)
        {
            return SystemCategories.IsTransferOutCategory(transaction.CategoryId);
        }

        public static bool IsIncomingTransfer(this TransactionDto transaction)
        {
            return SystemCategories.IsTransferInCategory(transaction.CategoryId);
        }

        public static TransactionDto GetFromTransaction(this TransactionDto transaction, TransactionDto pairedTransaction)
        {
            return transaction.IsOutgoingTransfer() ? transaction : pairedTransaction;
        }

        public static TransactionDto GetToTransaction(this TransactionDto transaction, TransactionDto pairedTransaction)
        {
            return transaction.IsOutgoingTransfer() ? pairedTransaction : transaction;
        }

        public static string GetAmountDisplay(this TransactionDto transaction)
        {
            if (transaction.Amount == 0) return "$0.00";

            if (transaction.TransactionType == TransactionTypeEnum.Transfer ||
                transaction.TransactionType == TransactionTypeEnum.CreditPayment)
            {
                return transaction.Amount.ToString("C");
            }

            return transaction.Amount.ToString("C");
        }

        public static string GetAmountDisplayWithSign(this TransactionDto transaction)
        {
            if (transaction.Amount == 0) return "$0.00";

            if (transaction.IsTransfer())
                return FormatWithSign(transaction.Amount);

            return transaction.Amount.ToString("C");
        }

        private static string FormatWithSign(decimal amount)
        {
            if (amount == 0) return "$0.00";
            return amount >= 0 ? $"+${amount:N2}" : $"-${Math.Abs(amount):N2}";
        }

        public static decimal GetSignedAmount(this TransactionDto transaction)
        {
            return (transaction.IsIncomingTransfer()
                            || transaction.IsIncome())
                                ? transaction.Amount
                                : (transaction.Amount * -1);
        }

        public static decimal GetAbsoluteAmountss(this TransactionDto transaction)
        {
            return Math.Abs(transaction.Amount);
        }
    }
}