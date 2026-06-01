using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Application.DTOs.Budgets
{
    public class BudgetFilterDto
    {
        /// <summary>
        /// Target year (e.g. 2026). If null, UI should assume current year.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Target month (1-12). If null, UI should assume current month.
        /// </summary>
        public int? Month { get; set; }

        /// <summary>
        /// Optional: filter budgets by a specific category.
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Optional: filter by category parent (useful when you group budgets by parent category).
        /// </summary>
        public int? ParentCategoryId { get; set; }

        /// <summary>
        /// Optional: filter by category type (Income / Expense). Transfer usually not used for budgets.
        /// </summary>
        public CategoryTypeEnum? Type { get; set; }

        /// <summary>
        /// Optional search (category name, notes, etc. depending on UI).
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// UI flag: show archived/deleted budgets if you ever add that screen later.
        /// </summary>
        public bool IncludeDeleted { get; set; } = false;
        public int ActiveTabIndex { get; set; } = 0;
        public bool OnlyWithActivity { get; set; } = false;
        public int ViewMode { get; set; } = 0;
        public bool ListExpanded { get; set; } = false;
        public int PaycheckView { get; set; } = 0;
    }
}
