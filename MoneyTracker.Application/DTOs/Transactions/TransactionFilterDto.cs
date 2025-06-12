using MoneyTracker.Domain.Enums;

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
        public int? CategoryId { get; set; }
        public CategoryType? Type { get; set; }
    }
}
