using MoneyTracker.Application.DTOs.Budgets;

namespace MoneyTracker.UI.Helpers
{
    public static class BudgetFilterHelper
    {
        public static BudgetFilterDto ResetToDefaults()
        {
            var now = DateTime.Today;

            return new BudgetFilterDto
            {
                Year = now.Year,
                Month = now.Month,
                IncludeDeleted = false
            };
        }

        public static bool HasActiveFilters(BudgetFilterDto filter)
        {
            var defaults = ResetToDefaults();

            return filter.Year != defaults.Year ||
                   filter.Month != defaults.Month ||
                   filter.CategoryId.HasValue ||
                   filter.ParentCategoryId.HasValue ||
                   filter.Type.HasValue ||
                   !string.IsNullOrWhiteSpace(filter.SearchText) ||
                   filter.IncludeDeleted != defaults.IncludeDeleted;
        }

        public static int GetActiveFilterCount(BudgetFilterDto filter)
        {
            var defaults = ResetToDefaults();
            int count = 0;

            if (filter.Year != defaults.Year) count++;
            if (filter.Month != defaults.Month) count++;
            if (filter.CategoryId.HasValue) count++;
            if (filter.ParentCategoryId.HasValue) count++;
            if (filter.Type.HasValue) count++;
            if (!string.IsNullOrWhiteSpace(filter.SearchText)) count++;
            if (filter.IncludeDeleted != defaults.IncludeDeleted) count++;

            return count;
        }
    }
}
