using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Enums;
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

        public static string ToDescription(this AccountType accountType)
        {
            return accountType switch
            {
                AccountType.Checking => "For everyday transactions and bill payments",
                AccountType.Savings => "For saving money and earning interest",
                AccountType.Credit => "For credit card purchases and payments",
                AccountType.Investment => "For stocks, bonds, and other investments",
                AccountType.Cash => "For physical cash and petty cash",
                _ => "General purpose account"
            };
        }



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

        public static string ToLabel(this AccountIcon accountIcon)
        {
            return accountIcon switch
            {
                AccountIcon.Wallet => "Wallet",
                AccountIcon.CreditCard => "Credit Card",
                AccountIcon.Savings => "Savings",
                _ => accountIcon.ToString()
            };
        }


        public static decimal GetAvailableCredit(this AccountDto account)
        {
            if (account.Type != AccountType.Credit || account.CreditLimit <= 0)
                return 0;

            return account.CreditLimit - Math.Abs(account.CurrentBalance);
        }

        public static bool IsOverLimit(this AccountDto account)
        {
            if (account.Type != AccountType.Credit || account.CreditLimit <= 0)
                return false;

            return Math.Abs(account.CurrentBalance) > account.CreditLimit;
        }

        public static string GetBalanceDisplayClass(this AccountDto account)
        {
            return account.CurrentBalance switch
            {
                > 0 => "text-success",
                < 0 => "text-error",
                _ => "text-muted"
            };
        }

        public static string GetAvailableCreditDisplayClass(this AccountDto account)
        {
            if (account.Type != AccountType.Credit || account.CreditLimit <= 0)
                return "text-muted";

            var isOverLimit = Math.Abs(account.CurrentBalance) > account.CreditLimit;
            return isOverLimit ? "text-error" : "text-success";
        }

        public static string GetCreditUtilizationText(this AccountDto account)
        {
            if (account.Type != AccountType.Credit || account.CreditLimit <= 0)
                return "N/A";

            var utilization = (Math.Abs(account.CurrentBalance) / account.CreditLimit) * 100;
            return $"{utilization:F1}%";
        }

        public static string GetCreditUtilizationClass(this AccountDto account)
        {
            if (account.Type != AccountType.Credit || account.CreditLimit <= 0)
                return "text-muted";

            var utilization = (Math.Abs(account.CurrentBalance) / account.CreditLimit) * 100;
            return utilization switch
            {
                <= 30 => "text-success",
                <= 70 => "text-warning",
                _ => "text-error"
            };
        }

        public static bool IsHealthy(this AccountDto account)
        {
            return account.Type switch
            {
                AccountType.Credit => Math.Abs(account.CurrentBalance) <= account.CreditLimit,
                AccountType.Checking => account.CurrentBalance >= 0,
                AccountType.Savings => account.CurrentBalance >= 0,
                AccountType.Investment => account.CurrentBalance >= 0,
                AccountType.Cash => account.CurrentBalance >= 0,
                _ => true
            };
        }

        public static string GetHealthStatusIcon(this AccountDto account)
        {
            return account.IsHealthy()
                ? Icons.Material.Filled.CheckCircle
                : Icons.Material.Filled.Warning;
        }

        public static string GetHealthStatusClass(this AccountDto account)
        {
            return account.IsHealthy() ? "text-success" : "text-warning";
        }


        public static string GetAccountSummary(this AccountDto account)
        {
            var summary = $"{account.Name} ({account.Type})";

            if (account.Type == AccountType.Credit && account.CreditLimit > 0)
            {
                summary += $" - Available: {account.AvailableCredit:C2}";
            }

            return summary;
        }

        public static string GetAccountDescription(this AccountDto account)
        {
            var description = account.Type.ToDescription();

            if (!string.IsNullOrEmpty(account.Notes))
            {
                description += $" | {account.Notes}";
            }

            return description;
        }

    }
}
