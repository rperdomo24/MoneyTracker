using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Enums.Category;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class CategoryExtensions
    {
        public static string GetDisplayName(this CategoryTypeEnum type) => type switch
        {
            CategoryTypeEnum.Income => "Income Categories",
            CategoryTypeEnum.Expense => "Expense Categories",
            _ => type.ToString()
        };

        public static string GetIcon(this CategoryTypeEnum type) => type switch
        {
            CategoryTypeEnum.Income => Icons.Material.Filled.TrendingUp,
            CategoryTypeEnum.Expense => Icons.Material.Filled.TrendingDown,
            _ => Icons.Material.Filled.Category
        };

        public static Color GetColor(this CategoryTypeEnum type) => type switch
        {
            CategoryTypeEnum.Income => Color.Success,
            CategoryTypeEnum.Expense => Color.Error,
            _ => Color.Primary
        };

        public static IEnumerable<IGrouping<CategoryTypeEnum, CategoryDto>> GroupByType(this IEnumerable<CategoryDto> categories)
        {
            return categories.GroupBy(c => c.Type).OrderBy(g => g.Key);
        }

        public static IEnumerable<CategoryDto> FilterByType(this IEnumerable<CategoryDto> categories, CategoryTypeEnum type)
        {
            return categories.Where(c => c.Type == type);
        }

    }
}
