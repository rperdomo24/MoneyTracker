using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.UI.Helpers
{
    public static class TransactionFilterHelper
    {
        public static bool HasActiveFilters(
            TransactionFilterDto filter,
            TimePeriodFilter defaultTimePeriod = TimePeriodFilter.ThisMonth,
            bool includeAccountFilter = true)
        {
            return filter.TimePeriod != defaultTimePeriod ||
                   (includeAccountFilter && filter.AccountIds?.Any() == true) ||
                   !string.IsNullOrWhiteSpace(filter.SearchText) ||
                   filter.CategoryIds?.Any() == true ||
                   filter.Types?.Any() == true;
        }

        public static TransactionFilterDto ResetToDefaults()
        {
            return new TransactionFilterDto
            {
                TimePeriod = TimePeriodFilter.ThisMonth
            };
        }

        public static int GetActiveFilterCount(
            TransactionFilterDto filter,
            TimePeriodFilter defaultTimePeriod = TimePeriodFilter.ThisMonth,
            bool includeAccountFilter = true)
        {
            int count = 0;

            if (filter.TimePeriod != defaultTimePeriod) count++;
            if (includeAccountFilter && filter.AccountIds?.Any() == true) count++;
            if (!string.IsNullOrWhiteSpace(filter.SearchText)) count++;
            if (filter.CategoryIds?.Any() == true) count++;
            if (filter.Types?.Any() == true) count++;

            return count;
        }
    }
}
