using MoneyTracker.Domain.Enums.Account;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class AccountIconExtensions
    {
        public static string ToIcon(this AccountIcon icon)
        {
            return icon switch
            {
                AccountIcon.Wallet => Icons.Material.Filled.Wallet,
                AccountIcon.AccountBalance => Icons.Material.Filled.AccountBalanceWallet,
                AccountIcon.Savings => Icons.Material.Filled.Savings,
                AccountIcon.CreditCard => Icons.Material.Filled.CreditCard,
                AccountIcon.Money => Icons.Material.Filled.Money,
                AccountIcon.AttachMoney => Icons.Material.Filled.AttachMoney,
                AccountIcon.Payments => Icons.Material.Filled.Payments,
                _ => Icons.Material.Filled.AccountBalanceWallet
            };
        }
    }
}
