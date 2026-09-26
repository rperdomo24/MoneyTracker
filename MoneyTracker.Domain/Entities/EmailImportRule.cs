using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class EmailImportRule : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(500)]
        public string SenderPattern { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string BankLabel { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? SubjectIncludeKeywords { get; set; }

        [MaxLength(500)]
        public string? SubjectExcludeKeywords { get; set; }

        public int? DefaultAccountId { get; set; }
        public Account? DefaultAccount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
