using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Enums;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class CategoryExtensions
    {
        public static string GetDisplayName(this CategoryType type) => type switch
        {
            CategoryType.Income => "Income Categories",
            CategoryType.Expense => "Expense Categories",
            _ => type.ToString()
        };

        public static string GetIcon(this CategoryType type) => type switch
        {
            CategoryType.Income => Icons.Material.Filled.TrendingUp,
            CategoryType.Expense => Icons.Material.Filled.TrendingDown,
            _ => Icons.Material.Filled.Category
        };

        public static Color GetColor(this CategoryType type) => type switch
        {
            CategoryType.Income => Color.Success,
            CategoryType.Expense => Color.Error,
            _ => Color.Primary
        };

        public static IEnumerable<IGrouping<CategoryType, CategoryDto>> GroupByType(this IEnumerable<CategoryDto> categories)
        {
            return categories.GroupBy(c => c.Type).OrderBy(g => g.Key);
        }

        public static IEnumerable<CategoryDto> FilterByType(this IEnumerable<CategoryDto> categories, CategoryType type)
        {
            return categories.Where(c => c.Type == type);
        }

    }
}
