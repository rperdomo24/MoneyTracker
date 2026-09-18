using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.Application.DTOs.Transactions
{
    public class TransactionFilterDto
    {
        public TimePeriodFilter TimePeriod { get; set; } = TimePeriodFilter.ThisMonth;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<int> AccountIds { get; set; } = new();
        public List<int> TransactionTypeIds { get; set; } = new();

        public string SearchText { get; set; } = string.Empty;
        public List<int> CategoryIds { get; set; } = new();
        public List<CategoryTypeEnum> Types { get; set; } = new();

        // Optional optimization for read-heavy dashboards where sort order is applied later.
        public bool SkipSorting { get; set; }

        public int RowsPerPage { get; set; } = 10;
    }
}
