using MoneyTracker.Domain.Enums.Budgets;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.UI.Helpers.VM
{
    public enum BudgetHealthState
    {
        NoBudget = 0,
        Healthy = 1,
        Warning = 2,
        OverBudget = 3
    }

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
        public bool HasBudget => Amount > 0;
        public bool IsOverBudget => HasBudget && Remaining < 0;
        public bool IsNearLimit => HasBudget && !IsOverBudget && ProgressPercent >= 80;
        public bool IncludeChildren { get; set; }
        public bool RolloverEnabled { get; set; }
        public string CategoryColor { get; set; } = "#757575";
        public CategoryIcon CategoryIcon { get; set; } = CategoryIcon.AccountBalanceWallet;
        public int? ParentCategoryId { get; set; }
        public int ChildrenCount { get; set; }

        public BudgetHealthState HealthState
        {
            get
            {
                if (!HasBudget) return BudgetHealthState.NoBudget;
                if (IsOverBudget) return BudgetHealthState.OverBudget;
                if (IsNearLimit) return BudgetHealthState.Warning;
                return BudgetHealthState.Healthy;
            }
        }

        public int ProgressPercent
        {
            get
            {
                if (Amount <= 0) return 0;

                var pct = (int)Math.Round((double)(Used / Amount) * 100d);
                return Math.Clamp(pct, 0, 100);
            }
        }
    }
}
