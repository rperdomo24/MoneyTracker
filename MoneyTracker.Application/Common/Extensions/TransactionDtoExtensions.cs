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

        // Agregar estos métodos a TransactionDtoExtensions.cs
        public static bool IsCreditPayment(this TransactionDto transaction)
        {
            return SystemCategories.IsCreditRelatedCategory(transaction.CategoryId);
            //return transaction.Category?.Name == "Credit Card Payment" &&
            //       transaction.Category?.Type == CategoryTypeEnum.Expense;
        }

        public static bool IsTransfer(this TransactionDto transaction)
        {
            return SystemCategories.IsTransferCategory(transaction.CategoryId);
            //return transaction.GetDerivedType() == TransactionTypeEnum.Transfer;
        }

        public static string GetAmountColorClass(this TransactionDto transaction)
        {
            if (transaction.IsIncome())
                return "text-success";
            else if (transaction.IsExpense())
                return "text-error";
            else
                return transaction.Category?.Color
                    ?? "text-default";
        }

        public static string GetTransactionColor(this TransactionDto transaction)
        {
            // Check if it's a credit payment first
            if (transaction.IsCreditPayment())
                return "#ff5722"; // Orange for credit payments

            if (transaction.Category?.Color != null)
                return transaction.Category.Color;

            return transaction.IsIncome() ? "#4caf50" : "#f44336";
        }

        public static string GetAmountDisplay(this TransactionDto transaction)
        {
            if (transaction.Amount <= 0) return "$0.00";

            if (transaction.TransactionType == TransactionTypeEnum.Transfer || transaction.TransactionType == TransactionTypeEnum.CreditPayment)
            {
                return transaction.Amount.ToString("C");
            }

            var sign = transaction.TransactionType == TransactionTypeEnum.Income ? "+" : "-";
            return $"{sign}{transaction.Amount:C}";


            //var prefix = transaction.IsIncome() ? "+" : "-";
            //return $"{prefix}{Math.Abs(transaction.Amount):C}";
            //if (transaction == null) return "$0.00";

            //var sign = transaction.Category?.Type == CategoryTypeEnum.Income ? "+" : "-";
            //return $"{sign}{transaction.Amount:C}";
        }

        public static string GetShortFormattedDate(this TransactionDto transaction)
        {
            return transaction.Date.ToString("MMM dd");
        }

        public static TransactionTypeEnum GetDerivedType(this TransactionDto transaction)
        {
            // Transfers = categorías del sistema 8-13
            if (transaction.CategoryId >= 8 && transaction.CategoryId <= 13)
                return TransactionTypeEnum.Transfer;

            // Income/Expense se deriva de la categoría
            return transaction.Category.Type == CategoryTypeEnum.Income
                ? TransactionTypeEnum.Income
                : TransactionTypeEnum.Expense;
        }
    }
}
