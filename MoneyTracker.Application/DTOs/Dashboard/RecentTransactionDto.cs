using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class RecentTransactionDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public string RelativeTime { get; set; } = string.Empty;
    }
}
