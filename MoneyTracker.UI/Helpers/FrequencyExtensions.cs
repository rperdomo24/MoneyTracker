using MoneyTracker.Domain.Enums.Loans;
using MoneyTracker.Domain.Enums.Transaction;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class FrequencyExtensions
    {
        /// <summary>Label used inside dropdowns (more descriptive than the short label shown in lists).</summary>
        public static string GetLabel(this RecurringFrequency frequency) => frequency switch
        {
            RecurringFrequency.Daily => "Daily",
            RecurringFrequency.Weekly => "Weekly",
            RecurringFrequency.BiWeekly => "Bi-Weekly (every 2 weeks)",
            RecurringFrequency.Monthly => "Monthly",
            RecurringFrequency.BiMonthly => "Bi-Monthly (every 2 months)",
            RecurringFrequency.Quarterly => "Quarterly",
            RecurringFrequency.Yearly => "Yearly",
            _ => frequency.ToString()
        };

        public static string GetIcon(this RecurringFrequency frequency) => frequency switch
        {
            RecurringFrequency.Daily => Icons.Material.Filled.Today,
            RecurringFrequency.Weekly or RecurringFrequency.BiWeekly => Icons.Material.Filled.DateRange,
            RecurringFrequency.Monthly or RecurringFrequency.BiMonthly => Icons.Material.Filled.CalendarMonth,
            RecurringFrequency.Quarterly => Icons.Material.Filled.EventRepeat,
            RecurringFrequency.Yearly => Icons.Material.Filled.CalendarToday,
            _ => Icons.Material.Filled.Repeat
        };

        public static string GetLabel(this PaymentFrequency frequency) => frequency switch
        {
            PaymentFrequency.Weekly => "Weekly",
            PaymentFrequency.Biweekly => "Biweekly (every 2 weeks)",
            PaymentFrequency.Monthly => "Monthly",
            _ => frequency.ToString()
        };

        public static string GetIcon(this PaymentFrequency frequency) => frequency switch
        {
            PaymentFrequency.Weekly or PaymentFrequency.Biweekly => Icons.Material.Filled.DateRange,
            _ => Icons.Material.Filled.CalendarMonth
        };
    }
}
