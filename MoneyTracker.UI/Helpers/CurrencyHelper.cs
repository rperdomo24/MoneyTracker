using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class CurrencyHelper
    {
        public static string FormatCurrency(decimal amount)
        {
            return amount < 0
                ? $"-${Math.Abs(amount):N2}"
                : $"${amount:N2}";
        }

        /// <summary>
        /// Gets MudBlazor color based on amount value
        /// </summary>
        public static Color GetAmountColor(decimal amount)
        {
            return amount switch
            {
                > 0 => Color.Success,
                < 0 => Color.Error,
                _ => Color.Default
            };
        }
        public static string FormatWithSign(decimal amount)
        {
            return amount >= 0 ? $"+${Math.Abs(amount):N2}" : $"-${Math.Abs(amount):N2}";
        }


    }
}