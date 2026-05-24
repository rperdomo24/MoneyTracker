namespace MoneyTracker.Application.DTOs.Auth
{
    public class RequestContextDto
    {
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string HttpMethod { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string? TraceId { get; init; }
    }
}
