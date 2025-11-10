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

        /// <summary>
        /// Gets CSS class for amount color
        /// </summary>
        public static string GetAmountColorClass(decimal amount)
        {
            return amount switch
            {
                > 0 => "text-success",
                < 0 => "text-error",
                _ => "text-default"
            };
        }

        /// <summary>
        /// Gets font weight class based on amount
        /// </summary>
        public static string GetAmountWeightClass(decimal amount, bool isBold = true)
        {
            return isBold ? "font-weight-bold" : "font-weight-medium";
        }
    }
}