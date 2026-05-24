using MoneyTracker.Domain.Enums.Budgets;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Application.DTOs.Budgets
{
    public class BudgetWithUsageDto
    {
        public BudgetDto Budget { get; set; } = new();

        public CategoryDto Category { get; set; } = new();

        public decimal Used { get; set; }
        public decimal DirectUsed { get; set; }
        public decimal Remaining => Budget.Amount - Used;

        public int ProgressPercent
        {
            get
            {
                if (Budget.Amount <= 0) return 0;
                var p = (int)Math.Round((double)(Used / Budget.Amount) * 100);
                return Math.Clamp(p, 0, 999);
            }
        }

        public bool HasChildren => Category.Children?.Any() == true;
        public int ChildrenCount => Category.Children?.Count ?? 0;
        public CategoryTypeEnum CategoryType => Category.Type;
        public string CategoryName => Category.Name;
        public string? CategoryColor => Category.Color;
        public CategoryIcon CategoryIcon => Category.Icon;
        public int? ParentCategoryId => Category.ParentId;
    }
}

