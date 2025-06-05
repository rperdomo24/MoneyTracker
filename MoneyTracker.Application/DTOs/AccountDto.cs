using MoneyTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Application.DTOs
{
    public class AccountDto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal Balance { get; set; } = 0;

        public decimal CreditLimit { get; set; } = 0;

        public string? Notes { get; set; }

        public AccountIcon Icon { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }

        public AccountType Type { get; set; }
    }
}
