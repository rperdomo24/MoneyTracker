using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.DTOs.Reports
{
    public class ReportTransactionDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsExpense { get; set; }
        public PaymentMethodEnum? PaymentMethod { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string? CardDisplayName { get; set; }
        public string Merchant { get; set; } = string.Empty;
    }
}
