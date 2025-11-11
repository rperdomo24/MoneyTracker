using MoneyTracker.Domain.Enums.Account;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class AccountTypeExtensions
    {
        public static string ToIcon(this AccountType accountType)
        {
            return accountType switch
            {
                AccountType.Checking => Icons.Material.Filled.AccountBalance,
                AccountType.Savings => Icons.Material.Filled.Savings,
                AccountType.Credit => Icons.Material.Filled.CreditCard,
                AccountType.Investment => Icons.Material.Filled.TrendingUp,
                AccountType.Cash => Icons.Material.Filled.AccountBalanceWallet,
                _ => Icons.Material.Filled.AccountBalance
            };
        }

        public static string ToLabel(this AccountType accountType)
        {
            return accountType switch
            {
                AccountType.Checking => "Checking Account",
                AccountType.Savings => "Savings Account",
                AccountType.Credit => "Credit Card",
                AccountType.Investment => "Investment Account",
                AccountType.Cash => "Cash Account",
                _ => accountType.ToString()
            };
        }
    }
}
