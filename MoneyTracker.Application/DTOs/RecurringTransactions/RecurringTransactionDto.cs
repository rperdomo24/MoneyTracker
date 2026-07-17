using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.DTOs.RecurringTransactions
{
    public class RecurringTransactionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public CategoryTypeEnum? CategoryType { get; set; }
        public int AccountId { get; set; }
        public string? AccountName { get; set; }
        public RecurringFrequency Frequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime NextDate { get; set; }
        public DateTime? LastGeneratedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? TotalOccurrences { get; set; }
        public int OccurrencesGenerated { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public PaymentMethodEnum? PaymentMethod { get; set; }
    }
}
