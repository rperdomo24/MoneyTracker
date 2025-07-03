using MoneyTracker.Application.Common.Extensions;
using MoneyTracker.Application.DTOs.Transactions;
using MudBlazor;

namespace MoneyTracker.UI.Helpers;

public static class TransactionHelpers
{

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