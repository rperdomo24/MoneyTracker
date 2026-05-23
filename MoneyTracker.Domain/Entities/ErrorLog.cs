using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class ErrorLog
    {
        public long Id { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(32)]
        public string Level { get; set; } = "Error";

        [Required]
        [MaxLength(512)]
        public string ExceptionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }

        [MaxLength(16)]
        public string? HttpMethod { get; set; }

        [MaxLength(1024)]
        public string? Path { get; set; }

        [MaxLength(2048)]
        public string? QueryString { get; set; }

        [MaxLength(128)]
        public string? TraceId { get; set; }

        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }

        [MaxLength(128)]
        public string? Environment { get; set; }
    }
}
