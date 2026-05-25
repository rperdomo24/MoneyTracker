using MoneyTracker.Domain.Enums.Account;

namespace MoneyTracker.Application.DTOs.Search
{
    public class GlobalSearchResultDto
    {
        public List<TransactionSearchItemDto> Transactions { get; set; } = [];
        public List<AccountSearchItemDto> Accounts { get; set; } = [];
        public List<CategorySearchItemDto> Categories { get; set; } = [];

        public bool HasResults =>
            Transactions.Count > 0 || Accounts.Count > 0 || Categories.Count > 0;
    }

    public class TransactionSearchItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int AccountId { get; set; }
        public string? CategoryName { get; set; }
    }

    public class AccountSearchItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public decimal Balance { get; set; }
        public string? BankName { get; set; }
    }

    public class CategorySearchItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int? ParentId { get; set; }
    }
}
