using MoneyTracker.Application.Common.Extensions;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.DTOs.Transactions
{
    public class TransactionDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        public string Description { get; set; }

        public int CategoryId { get; set; }

        public int AccountId { get; set; }

        public PaymentMethodEnum? PaymentMethod { get; set; }

        // For scheduled transactions
        public TransactionStatus Status { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public bool IsSystemGenerated { get; set; }

        // For transfers
        public int? TransferPairId { get; set; }

        public TransferTypeEnum? TransferType { get; set; }

        public TransactionTypeEnum TransactionType
        {
            get
            {
                if (CategoryId <= 0)
                {
                    return TransactionTypeEnum.Expense;
                }
                else
                {
                    return this.GetDerivedType();
                }
            }
        }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public AccountDto Account { get; set; }

        public CategoryDto? Category { get; set; }

    }
}
