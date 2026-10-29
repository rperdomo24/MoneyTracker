using MoneyTracker.Domain.Enums.Filters;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class TimePeriodFilterExtensions
    {
        public static string GetLabel(this TimePeriodFilter period) => period switch
        {
            TimePeriodFilter.AllTime => "All Time",
            TimePeriodFilter.Last7Days => "Last 7 Days",
            TimePeriodFilter.Last30Days => "Last 30 Days",
            TimePeriodFilter.Last90Days => "Last 90 Days",
            TimePeriodFilter.LastMonth => "Last Month",
            TimePeriodFilter.ThisMonth => "This Month",
            TimePeriodFilter.Q1ThisMonth => "Q1 — 1st to 15th",
            TimePeriodFilter.Q2ThisMonth => "Q2 — 16th to End",
            TimePeriodFilter.ThisYear => "This Year",
            TimePeriodFilter.Custom => "Custom Range",
            _ => period.ToString()
        };

        public static string GetIcon(this TimePeriodFilter period) => period switch
        {
            TimePeriodFilter.AllTime => Icons.Material.Filled.AllInclusive,
            TimePeriodFilter.Last7Days => Icons.Material.Filled.Today,
            TimePeriodFilter.Last30Days => Icons.Material.Filled.DateRange,
            TimePeriodFilter.Last90Days => Icons.Material.Filled.EventNote,
            TimePeriodFilter.LastMonth => Icons.Material.Filled.History,
            TimePeriodFilter.ThisMonth => Icons.Material.Filled.CalendarMonth,
            TimePeriodFilter.Q1ThisMonth => Icons.Material.Filled.LooksOne,
            TimePeriodFilter.Q2ThisMonth => Icons.Material.Filled.LooksTwo,
            TimePeriodFilter.ThisYear => Icons.Material.Filled.CalendarToday,
            TimePeriodFilter.Custom => Icons.Material.Filled.Tune,
            _ => Icons.Material.Filled.Schedule
        };
    }
}
