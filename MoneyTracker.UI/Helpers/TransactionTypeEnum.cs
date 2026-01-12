using MoneyTracker.Domain.Enums.Transaction;
using MudBlazor;
using System.ComponentModel;

namespace MoneyTracker.UI.Helpers
{
    public static class TransactionTypeExtensions
    {
        public static string GetTransactionypeIcon(this TransactionTypeEnum type) => type switch
        {
            TransactionTypeEnum.Income => Icons.Material.Filled.TrendingUp,
            TransactionTypeEnum.Expense => Icons.Material.Filled.TrendingDown,
            TransactionTypeEnum.Transfer => Icons.Material.Filled.SwapHoriz,
            _ => Icons.Material.Filled.AttachMoney
        };

        public static Color GetTransactionTypeColor(this TransactionTypeEnum type) => type switch
        {
            TransactionTypeEnum.Income => Color.Success,
            TransactionTypeEnum.Expense => Color.Error,
            TransactionTypeEnum.Transfer => Color.Surface,
            _ => Color.Primary
        };

        public static string GetTransactionTypeHexColor(this TransactionTypeEnum type) => type switch
        {
            TransactionTypeEnum.Income => "#4caf50",
            TransactionTypeEnum.Expense => "#f44336",
            TransactionTypeEnum.Transfer => "#2196f3",
            _ => "#6366f1"
        };

        public static string GetDescription(this TransactionTypeEnum transactionType)
        {
            var fieldInfo = transactionType.GetType().GetField(transactionType.ToString());
            var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0
                ? ((DescriptionAttribute)attributes[0]).Description
                : transactionType.ToString();
        }
    }
}
