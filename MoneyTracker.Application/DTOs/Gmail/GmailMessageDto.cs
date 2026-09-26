namespace MoneyTracker.Application.DTOs.Gmail
{
    public sealed class GmailMessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string? ThreadId { get; set; }
        public string From { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime ReceivedAtUtc { get; set; }
        public string BodyText { get; set; } = string.Empty;
    }
}
