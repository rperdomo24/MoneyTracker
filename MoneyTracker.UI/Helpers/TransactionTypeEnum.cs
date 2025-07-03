using MoneyTracker.Domain.Enums.Transaction;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class TransactionTypeExtensions
    {
        public static string GetTransactionypeIcon(this TransactionTypeEnum type) => type switch
        {
            TransactionTypeEnum.Income => Icons.Material.Filled.TrendingUp,
            TransactionTypeEnum.Expense => Icons.Material.Filled.TrendingDown,
            TransactionTypeEnum.Transfer => Icons.Material.Filled.SwapHoriz,
            TransactionTypeEnum.CreditPayment => Icons.Material.Filled.CreditCard,
            _ => Icons.Material.Filled.AttachMoney
        };

        public static Color GetTransactionTypeColor(this TransactionTypeEnum type) => type switch
        {
            TransactionTypeEnum.Income => Color.Success,
            TransactionTypeEnum.Expense => Color.Error,
            TransactionTypeEnum.Transfer => Color.Surface,
            TransactionTypeEnum.CreditPayment => Color.Warning,
            _ => Color.Primary
        };

        public static string GetTransactionTypeHexColor(this TransactionTypeEnum type) => type switch
        {
            TransactionTypeEnum.Income => "#4caf50",
            TransactionTypeEnum.Expense => "#f44336",
            TransactionTypeEnum.Transfer => "#2196f3",
            TransactionTypeEnum.CreditPayment => "#ff5722",
            _ => "#6366f1"
        };
    }
}
