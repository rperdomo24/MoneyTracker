using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class UserVerificationCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(32)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string CodeHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime ResendAvailableAtUtc { get; set; }

        public DateTime? InvalidatedAtUtc { get; set; }
        public DateTime? ConsumedAtUtc { get; set; }

        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 3;
    }
}
