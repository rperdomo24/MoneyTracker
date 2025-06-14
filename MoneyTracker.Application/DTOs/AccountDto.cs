using MoneyTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTracker.Application.DTOs
{
    public class AccountDto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        //public decimal CurrentBalance { get; set; } = 0;

        public decimal CreditLimit { get; set; } = 0;

        public decimal CurrentBalance { get; set; } = 0;

        public string? Notes { get; set; }

        public AccountIcon Icon { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }

        public AccountType Type { get; set; }

        public decimal AvailableCredit => Type == AccountType.Credit
            ? CreditLimit - Math.Abs(CurrentBalance)
            : 0;

        public bool IsOverLimit => Type == AccountType.Credit
            && Math.Abs(CurrentBalance) > CreditLimit;

        public string FormattedBalance => CurrentBalance.ToString("C2");

        public string BalanceColorClass => CurrentBalance >= 0
            ? "text-green-600"
            : "text-red-600";
    }
}
