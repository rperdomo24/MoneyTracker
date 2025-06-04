using MoneyTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public TransactionType Type { get; set; }

        public int? AccountId { get; set; }
        public Account? Account { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
