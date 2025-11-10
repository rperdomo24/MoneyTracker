using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.UI.Helpers
{
    public static class TransactionFilterHelper
    {
        /// <summary>
        /// Checks if any filters are active beyond default values
        /// </summary>
        public static bool HasActiveFilters(TransactionFilterDto filter)
        {
            return filter.TimePeriod != TimePeriodFilter.ThisMonth ||
                   filter.AccountIds?.Any() == true ||
                   !string.IsNullOrWhiteSpace(filter.SearchText) ||
                   filter.CategoryId.HasValue ||
                   filter.Type.HasValue;
        }

        /// <summary>
        /// Checks if specific account filter is active
        /// </summary>
        public static bool HasAccountFilter(TransactionFilterDto filter, int accountId)
        {
            return filter.AccountIds?.Contains(accountId) == true;
        }

        /// <summary>
        /// Ensures account filter includes specific account ID
        /// </summary>
        public static void EnsureAccountFilter(TransactionFilterDto filter, int accountId)
        {
            if (filter.AccountIds?.Contains(accountId) != true)
            {
                filter.AccountIds = new List<int> { accountId };
            }
        }

        /// <summary>
        /// Resets all filters to default values
        /// </summary>
        public static TransactionFilterDto ResetToDefaults()
        {
            return new TransactionFilterDto
            {
                TimePeriod = TimePeriodFilter.ThisMonth
            };
        }

        /// <summary>
        /// Gets count of active filters
        /// </summary>
        public static int GetActiveFilterCount(TransactionFilterDto filter)
        {
            int count = 0;

            if (filter.TimePeriod != TimePeriodFilter.ThisMonth) count++;
            if (filter.AccountIds?.Any() == true) count++;
            if (!string.IsNullOrWhiteSpace(filter.SearchText)) count++;
            if (filter.CategoryId.HasValue) count++;
            if (filter.Type.HasValue) count++;

            return count;
        }
    }
}
