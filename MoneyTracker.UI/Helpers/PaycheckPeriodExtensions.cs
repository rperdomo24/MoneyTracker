using MoneyTracker.Domain.Enums.Budgets;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class PaycheckPeriodExtensions
    {
        public static string GetLabel(this PaycheckPeriod period) => period switch
        {
            PaycheckPeriod.Both => "Both paychecks (split ÷2)",
            PaycheckPeriod.FirstOnly => "First paycheck only (days 1-15)",
            PaycheckPeriod.SecondOnly => "Second paycheck only (days 16-end)",
            _ => period.ToString()
        };

        public static string GetIcon(this PaycheckPeriod period) => period switch
        {
            PaycheckPeriod.FirstOnly => Icons.Material.Filled.LooksOne,
            PaycheckPeriod.SecondOnly => Icons.Material.Filled.LooksTwo,
            _ => Icons.Material.Filled.CalendarViewMonth
        };
    }
}
