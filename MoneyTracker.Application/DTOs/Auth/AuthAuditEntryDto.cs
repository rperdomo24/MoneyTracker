namespace MoneyTracker.Application.DTOs.Auth
{
    public class AuthAuditEntryDto
    {
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public string? EmailMasked { get; set; }
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? HttpMethod { get; set; }
        public string? Path { get; set; }
        public string? TraceId { get; set; }
    }
}
