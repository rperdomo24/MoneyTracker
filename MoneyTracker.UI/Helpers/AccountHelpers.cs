using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Enums;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class AccountHelpers
    {
        public static string GetAvailableCreditString(this AccountDto account)
        {
            if (account.Type != AccountType.Credit || account.CreditLimit <= 0)
               return "$0.00";

            var available = account.CreditLimit - Math.Abs(account.CurrentBalance);
            return available.ToString("C2");
        }

        public static string GetUtilizationPercentage(this AccountDto account)
        {
            if (account == null || account.CreditLimit == 0) return "0%";
            var percent = (account.CurrentBalance / account.CreditLimit) * 100;
            return Math.Round(percent, 1).ToString("0.#") + "%";
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
