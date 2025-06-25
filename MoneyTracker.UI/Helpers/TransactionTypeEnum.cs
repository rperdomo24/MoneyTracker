using MoneyTracker.Domain.Enums;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class TransactionTypeExtensions
    {
        public static string GetTransactionypeIcon(this TransactionType type) => type switch
        {
            TransactionType.Income => Icons.Material.Filled.TrendingUp,
            TransactionType.Expense => Icons.Material.Filled.TrendingDown,
            TransactionType.Transfer => Icons.Material.Filled.SwapHoriz,
            TransactionType.CreditPayment => Icons.Material.Filled.CreditCard,
            _ => Icons.Material.Filled.AttachMoney
        };

        public static Color GetTransactionTypeColor(this TransactionType type) => type switch
        {
            TransactionType.Income => Color.Success,
            TransactionType.Expense => Color.Error,
            TransactionType.Transfer => Color.Info,
            TransactionType.CreditPayment => Color.Warning,
            _ => Color.Primary
        };

        public static string GetTransactionTypeHexColor(this TransactionType type) => type switch
        {
            TransactionType.Income => "#4caf50",
            TransactionType.Expense => "#f44336",
            TransactionType.Transfer => "#2196f3",
            TransactionType.CreditPayment => "#ff5722",
            _ => "#6366f1"
        };
    }
}
