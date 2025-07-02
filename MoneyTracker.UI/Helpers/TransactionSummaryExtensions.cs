using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Domain.Enums.Transaction;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class TransactionSummaryExtensions
    {
        public static string GetBalanceCardStyle(this TransactionSummaryDto summary)
        {
            var color = summary.Balance >= 0 ? TransactionType.Income.GetTransactionTypeHexColor() : TransactionType.Expense.GetTransactionTypeHexColor();
            return $"border-top: 4px solid {color};";
        }

        public static string GetBalanceIcon(this TransactionSummaryDto summary)
        {
            return summary.Balance >= 0 ? Icons.Material.Filled.AccountBalance : Icons.Material.Filled.Warning;
        }

        public static Color GetBalanceColor(this TransactionSummaryDto summary)
        {
            return summary.Balance >= 0 ? Color.Success : Color.Error;
        }

        public static string GetBalanceTextClass(this TransactionSummaryDto summary)
        {
            return $"font-weight-bold {(summary.Balance >= 0 ? "text-success" : "text-error")}";
        }

        public static decimal GetAverageAmount(this TransactionSummaryDto summary)
        {
            return summary.TotalCount > 0 ? (summary.TotalIncome + summary.TotalExpense) / summary.TotalCount : 0;
        }

        public static string GetTransactionCountText(this TransactionSummaryDto summary)
        {
            return $"{summary.TotalCount} transaction{(summary.TotalCount != 1 ? "s" : "")}";
        }
    }

}
