using MoneyTracker.Domain.Enums.Account;

namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class AccountActivityDto
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public AccountIcon Icon { get; set; }
        public string Color { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalVolume { get; set; }
        public DateTime? LastActivity { get; set; }
        public string ActivityLevel { get; set; } = string.Empty;
        public decimal CreditUtilization { get; set; }
    }
}
