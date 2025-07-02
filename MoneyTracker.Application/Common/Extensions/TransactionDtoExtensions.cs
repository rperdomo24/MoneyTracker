using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Application.Common.Extensions
{
    public static class TransactionDtoExtensions
    {
        public static bool IsIncome(this TransactionDto transaction)
        {
            return transaction.Category?.Type == CategoryType.Income;
        }

        public static bool IsExpense(this TransactionDto transaction)
        {
            return transaction.Category?.Type == CategoryType.Expense;
        }

        // Agregar estos métodos a TransactionDtoExtensions.cs
        public static bool IsCreditPayment(this TransactionDto transaction)
        {
            return transaction.Category?.Name == "Credit Card Payment" &&
                   transaction.Category?.Type == CategoryType.Expense;
        }

        public static bool IsTransfer(this TransactionDto transaction)
        {
            return (transaction.Category?.Name == "Transfer Out" ||
                    transaction.Category?.Name == "Transfer In") &&
                   (transaction.Category?.Type == CategoryType.Expense ||
                    transaction.Category?.Type == CategoryType.Income);
        }

        public static string GetAmountColorClass(this TransactionDto transaction)
        {
            if (transaction.IsIncome())
                return "text-success";
            else if (transaction.IsExpense())
                return "text-error";
            else
                return "text-default";
        }

        public static string GetAmountDisplay(this TransactionDto transaction)
        {
            var prefix = transaction.IsIncome() ? "+" : "-";
            return $"{prefix}{Math.Abs(transaction.Amount):C}";
        }

        public static string GetShortFormattedDate(this TransactionDto transaction)
        {
            return transaction.Date.ToString("MMM dd");
        }
    }
}
