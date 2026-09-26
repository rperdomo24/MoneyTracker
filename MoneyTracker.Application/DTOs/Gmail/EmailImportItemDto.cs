using MoneyTracker.Domain.Enums;

namespace MoneyTracker.Application.DTOs.Gmail
{
    public class EmailImportItemDto
    {
        public int Id { get; set; }
        public string From { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public DateTime ReceivedAtUtc { get; set; }
        public string BodyText { get; set; } = string.Empty;
        public EmailImportStatus Status { get; set; }
        public decimal? Amount { get; set; }
        public string? Provider { get; set; }
        public int? TransactionId { get; set; }
    }
}
