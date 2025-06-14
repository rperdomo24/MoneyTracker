using MoneyTracker.Application.DTOs;

namespace MoneyTracker.UI.Helpers
{
    public static class AccountHelpers
    {
        public static string GetAvailableCredit(this AccountDto account)
        {
            if (account == null) return "$0.00";
            var available = account.CreditLimit - account.CurrentBalance;
            return available.ToString("C2");
        }

        public static string GetUtilizationPercentage(this AccountDto account)
        {
            if (account == null || account.CreditLimit == 0) return "0%";
            var percent = (account.CurrentBalance / account.CreditLimit) * 100;
            return Math.Round(percent, 1).ToString("0.#") + "%";
        }
    }

}
