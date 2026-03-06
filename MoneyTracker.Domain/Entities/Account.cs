using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class Account : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal Balance { get; set; } = 0;

        public decimal CreditLimit { get; set; } = 0;

        public string? Notes { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }

        public AccountType Type { get; set; }
    }
}
