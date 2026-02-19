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
        public decimal Used { get; set; }

        public decimal Remaining => Amount - Used;

        public bool IncludeChildren { get; set; }
        public bool RolloverEnabled { get; set; }

        public string CategoryColor { get; set; } = "#757575";
        public CategoryIcon CategoryIcon { get; set; } = CategoryIcon.AccountBalanceWallet;
        public int? ParentCategoryId { get; set; }
        public int ChildrenCount { get; set; }

        /// <summary>
        /// 0..100 for UI progress components.
        /// If Amount is 0 => 0.
        /// If Used exceeds Amount => 100 (color will indicate over/near).
        /// </summary>
        public int ProgressPercent
        {
            get
            {
                if (Amount <= 0) return 0;

                var pct = (int)Math.Round((double)(Used / Amount) * 100d);

                // UI should never go above 100. "Over" is indicated by Remaining < 0 + Color.Error.
                return Math.Clamp(pct, 0, 100);
            }
        }
    }
}
