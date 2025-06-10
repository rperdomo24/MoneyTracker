using Microsoft.Extensions.Configuration.UserSecrets;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Enums;
using MudBlazor;

namespace MoneyTracker.UI.Helpers;

public static class TransactionHelpers
{
    #region Amount Display Methods

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
    /// Obtiene el display del monto sin null checking (para cuando estés seguro que no es null)
    /// </summary>
    public static string GetAmountDisplay(this TransactionDto transaction)
    {
        var sign = transaction.Category?.Type == CategoryType.Income ? "+" : "-";
        return $"{sign}{transaction.Amount:C}";
    }

    public static string GetTransactionSubtitle(this TransactionDto transaction)
    {
        if (transaction == null) return "";
        return $"{transaction.Category?.Name} • {transaction.GetShortFormattedDate()}";
    }

    #endregion

    #region CSS Classes

    /// <summary>
    /// Obtiene la clase CSS para el color del monto (text-success/text-error) con null safety
    /// </summary>
    public static string GetAmountColorClassSafe(this TransactionDto? transaction)
    {
        if (transaction?.Category?.Type == CategoryType.Income)
            return "text-success";
        return transaction?.Category?.Type == CategoryType.Expense ? "text-error" : "";
    }

    /// <summary>
    /// Obtiene la clase CSS para el color del monto sin null checking
    /// </summary>
    public static string GetAmountColorClass(this TransactionDto transaction)
    {
        return transaction.Category?.Type == CategoryType.Income ? "text-success" : "text-error";
    }

    #endregion

    #region Icons and Colors

    /// <summary>
    /// Obtiene el ícono de Material Design para la transacción con null safety
    /// </summary>
    public static string GetTransactionIconSafe(this TransactionDto? transaction)
    {
        if (transaction?.Category?.Type == CategoryType.Income)
            return Icons.Material.Filled.TrendingUp;
        return Icons.Material.Filled.TrendingDown;
    }

    /// <summary>
    /// Obtiene el ícono de Material Design para la transacción
    /// </summary>
    public static string GetTransactionIcon(this TransactionDto transaction)
    {
        return transaction.Category?.Type == CategoryType.Income
            ? Icons.Material.Filled.TrendingUp
            : Icons.Material.Filled.TrendingDown;
    }

    /// <summary>
    /// Obtiene el color hexadecimal para la transacción con null safety
    /// </summary>
    public static string GetTransactionColorSafe(this TransactionDto? transaction)
    {
        if (transaction?.Category?.Type == CategoryType.Income)
            return "#4CAF50"; // Verde
        return "#f44336"; // Rojo
    }

    /// <summary>
    /// Obtiene el color hexadecimal para la transacción
    /// </summary>
    public static string GetTransactionColor(this TransactionDto transaction)
    {
        return transaction.Category?.Type == CategoryType.Income ? "#4CAF50" : "#f44336";
    }

    /// <summary>
    /// Obtiene el Color enum de MudBlazor para la transacción con null safety
    /// </summary>
    public static Color GetTransactionMudColorSafe(this TransactionDto? transaction)
    {
        if (transaction?.Category?.Type == CategoryType.Income)
            return Color.Success;
        return Color.Error;
    }

    /// <summary>
    /// Obtiene el Color enum de MudBlazor para la transacción
    /// </summary>
    public static Color GetTransactionMudColor(this TransactionDto transaction)
    {
        return transaction.Category?.Type == CategoryType.Income ? Color.Success : Color.Error;
    }

    #endregion

    #region Transaction Type Helpers

    /// <summary>
    /// Determina si la transacción es un ingreso
    /// </summary>
    public static bool IsIncome(this TransactionDto? transaction)
    {
        return transaction?.Category?.Type == CategoryType.Income;
    }

    /// <summary>
    /// Determina si la transacción es un gasto
    /// </summary>
    public static bool IsExpense(this TransactionDto? transaction)
    {
        return transaction?.Category?.Type == CategoryType.Expense;
    }

    /// <summary>
    /// Obtiene el ícono para el tipo de categoría
    /// </summary>
    public static string GetCategoryTypeIcon(this CategoryType categoryType)
    {
        return categoryType == CategoryType.Income
            ? Icons.Material.Filled.TrendingUp
            : Icons.Material.Filled.TrendingDown;
    }

    /// <summary>
    /// Obtiene el color MudBlazor para el tipo de categoría
    /// </summary>
    public static Color GetCategoryTypeMudColor(this CategoryType categoryType)
    {
        return categoryType == CategoryType.Income ? Color.Success : Color.Error;
    }

    #endregion

    #region Transaction-Specific Helpers

    /// <summary>
    /// Obtiene una fecha formateada específica para transacciones (usa DateTimeHelpers)
    /// </summary>
    public static string GetFormattedDate(this TransactionDto transaction)
    {
        return transaction.Date.GetFormattedDate();
    }

    /// <summary>
    /// Obtiene una fecha formateada corta específica para transacciones (usa DateTimeHelpers)
    /// </summary>
    public static string GetShortFormattedDate(this TransactionDto transaction)
    {
        return transaction.Date.GetShortFormattedDate();
    }

    /// <summary>
    /// Obtiene fecha con día de la semana para transacciones
    /// </summary>
    public static string GetFormattedDateWithDay(this TransactionDto transaction)
    {
        return transaction.Date.GetFormattedDateWithDay();
    }

    /// <summary>
    /// Determina si la transacción fue modificada después de creada
    /// </summary>
    public static bool WasModified(this TransactionDto transaction)
    {
        return transaction.UpdatedAt != transaction.CreatedAt;
    }

    /// <summary>
    /// Determina si la transacción es de los últimos N días (usa DateTimeHelpers)
    /// </summary>
    public static bool IsFromLastDays(this TransactionDto transaction, int days)
    {
        return transaction.Date.IsFromLastDays(days);
    }

    /// <summary>
    /// Determina si la transacción es de hoy
    /// </summary>
    public static bool IsToday(this TransactionDto transaction)
    {
        return transaction.Date.IsToday();
    }

    /// <summary>
    /// Determina si la transacción es de este mes
    /// </summary>
    public static bool IsThisMonth(this TransactionDto transaction)
    {
        return transaction.Date.IsThisMonth();
    }

    /// <summary>
    /// Determina si el monto es similar a otro monto (usa MathHelpers)
    /// </summary>
    public static bool IsSimilarAmount(this TransactionDto transaction, decimal otherAmount, decimal tolerance = 0.1m)
    {
        return MathHelpers.IsSimilarAmount(transaction.Amount, otherAmount, tolerance);
    }

    /// <summary>
    /// Obtiene el tiempo relativo de la transacción
    /// </summary>
    public static string GetRelativeTime(this TransactionDto transaction)
    {
        return transaction.Date.GetRelativeTime();
    }

    #endregion

    #region Static Helpers for TransactionType enum

    /// <summary>
    /// Obtiene el ícono para un TransactionType
    /// </summary>
    public static string GetIcon(this TransactionType transactionType)
    {
        return transactionType == TransactionType.Income
            ? Icons.Material.Filled.TrendingUp
            : Icons.Material.Filled.TrendingDown;
    }

    /// <summary>
    /// Obtiene el color MudBlazor para un TransactionType
    /// </summary>
    public static Color GetMudColor(this TransactionType transactionType)
    {
        return transactionType == TransactionType.Income ? Color.Success : Color.Error;
    }

    /// <summary>
    /// Obtiene el color hexadecimal para un TransactionType
    /// </summary>
    public static string GetHexColor(this TransactionType transactionType)
    {
        return transactionType == TransactionType.Income ? "#4CAF50" : "#f44336";
    }

    #endregion
}