using MoneyTracker.Domain.Enums;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class AccountTypeExtensions
    {
        public static string ToLabel(this AccountType type) => type switch
        {
            AccountType.Cash => "Cash",
            AccountType.Bank => "Bank Account",
            AccountType.Credit => "Credit Card",
            _ => "Unknown"
        };

        public static string ToIcon(this AccountType type) => type switch
        {
            AccountType.Cash => Icons.Material.Filled.AttachMoney,
            AccountType.Bank => Icons.Material.Filled.AccountBalance,
            AccountType.Credit => Icons.Material.Filled.CreditCard,
            _ => Icons.Material.Filled.HelpOutline
        };
    }
}
