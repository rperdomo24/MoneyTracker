using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class UserVerificationCodeAudit
    {
        public long Id { get; set; }
        public Guid? VerificationCodeId { get; set; }
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(32)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string EventType { get; set; } = string.Empty;

        public bool Success { get; set; }
        public int AttemptCount { get; set; }
        public DateTime EventAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(256)]
        public string? Message { get; set; }
    }
}
