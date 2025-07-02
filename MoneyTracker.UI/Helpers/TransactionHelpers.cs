using MoneyTracker.Application.DTOs.Transactions;
using MudBlazor;
using MoneyTracker.Application.Common.Extensions;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.UI.Helpers;

public static class TransactionHelpers
{
    /// <summary>
    /// Obtiene el display del monto con signo (+/-) basado en el tipo de categoría
    /// </summary>
    public static string GetAmountDisplaySafe(this TransactionDto? transaction)
    {
        if (transaction == null) return "$0.00";

        var sign = transaction.Category?.Type == CategoryType.Income ? "+" : "-";
        return $"{sign}{transaction.Amount:C}";
    }

    /// <summary>
    /// Obtiene el color hexadecimal para la transacción
    /// </summary>

    public static string GetTransactionColor(this TransactionDto transaction)
    {
        // Check if it's a credit payment first
        if (transaction.IsCreditPayment())
            return "#ff5722"; // Orange for credit payments

        if (transaction.Category?.Color != null)
            return transaction.Category.Color;

        return transaction.IsIncome() ? "#4caf50" : "#f44336";
    }

    public static string GetTransactionIcon(this TransactionDto transaction)
    {
        // Check if it's a credit payment first
        if (transaction.IsCreditPayment())
            return Icons.Material.Filled.CreditCard;

        if (transaction.Category?.Icon != null)
            return transaction.Category.Icon.ToIcon();

        return transaction.IsIncome() ? Icons.Material.Filled.TrendingUp : Icons.Material.Filled.TrendingDown;
    }

}