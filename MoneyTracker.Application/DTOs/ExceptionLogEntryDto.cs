namespace MoneyTracker.Application.DTOs
{
    public class ExceptionLogEntryDto
    {
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = "Error";
        public string ExceptionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
        public string? HttpMethod { get; set; }
        public string? Path { get; set; }
        public string? QueryString { get; set; }
        public string? TraceId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string? Environment { get; set; }
    }
}
