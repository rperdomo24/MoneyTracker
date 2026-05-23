using MoneyTracker.Application.Common.Extensions;

namespace MoneyTracker.Application.DTOs.Transactions
{
    public class TransactionGroupDto
    {
        public DateTime Date { get; set; }
        public string DateDisplay => Date.ToString("MMMM dd, yyyy");
        public List<TransactionDto> Transactions { get; set; } = new();
        public int Count => Transactions.Count;
        public decimal Total => Transactions.Sum(t => t.Amount);
        public List<int> TransactionIds => Transactions.Select(t => t.Id).ToList();
    }
}
