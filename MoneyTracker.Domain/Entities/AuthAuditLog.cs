using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class AuthAuditLog
    {
        public long Id { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(64)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(16)]
        public string Outcome { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? FailureReason { get; set; }

        [MaxLength(128)]
        public string? EmailMasked { get; set; }

        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        [MaxLength(1024)]
        public string? UserAgent { get; set; }

        [MaxLength(16)]
        public string? HttpMethod { get; set; }

        [MaxLength(1024)]
        public string? Path { get; set; }

        [MaxLength(128)]
        public string? TraceId { get; set; }
    }
}
