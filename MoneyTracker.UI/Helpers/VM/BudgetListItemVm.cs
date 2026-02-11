using MoneyTracker.Domain.Enums.Budgets;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.UI.Helpers.VM
{
    public class BudgetListItemVm
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public CategoryTypeEnum CategoryType { get; set; }
        public RolloverMode RolloverMode { get; set; }
        public decimal Amount { get; set; }
        public decimal Used { get; set; } // Fase 4 lo calculamos
        public decimal Remaining => Amount - Used;

        public bool IncludeChildren { get; set; }
        public bool RolloverEnabled { get; set; }

        public string CategoryColor { get; set; } = "#757575";
        public CategoryIcon CategoryIcon { get; set; } = CategoryIcon.AccountBalanceWallet;
        public int? ParentCategoryId { get; set; }
        public int ChildrenCount { get; set; }

        public int ProgressPercent
        {
            get
            {
                if (Amount <= 0) return 0;
                var p = (int)Math.Round((double)(Used / Amount) * 100);
                return Math.Clamp(p, 0, 999);
            }
        }
    }
}
