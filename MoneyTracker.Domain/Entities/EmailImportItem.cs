using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class EmailImportItem : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string GmailMessageId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ThreadId { get; set; }

        [Required]
        [MaxLength(320)]
        public string From { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Subject { get; set; }

        public DateTime ReceivedAtUtc { get; set; }

        [Required]
        public string BodyText { get; set; } = string.Empty;

        [MaxLength(64)]
        public string BodyHash { get; set; } = string.Empty;

        public string? ParsedJson { get; set; }

        [MaxLength(200)]
        public string? Fingerprint { get; set; }

        public EmailImportStatus Status { get; set; } = EmailImportStatus.Pending;

        public int? TransactionId { get; set; }
        public int? AiTrainingDataId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
