namespace MoneyTracker.Application.DTOs.Transactions
{
    public class TransactionDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        public CategoryDto? Category { get; set; }

        public int? AccountId { get; set; }
        public AccountDto Account { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
