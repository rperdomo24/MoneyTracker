using MoneyTracker.Domain.Enums.Account;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Application.DTOs
{
    public class AccountDto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal CreditLimit { get; set; } = 0;

        public decimal CurrentBalance { get; set; } = 0;

        public string? Notes { get; set; }

        public AccountIcon Icon { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }

        public AccountType Type { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(150)]
        public string? CardDisplayName { get; set; }


        // View Model properties
        public decimal AvailableCredit => Type == AccountType.Credit
            ? CreditLimit - Math.Abs(CurrentBalance)
            : 0;

        public bool IsOverLimit => Type == AccountType.Credit
            && Math.Abs(CurrentBalance) > CreditLimit;

    }
}
