namespace MoneyTracker.Application.DTOs.Auth
{
    public class VerificationCodeIssueDto
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime ResendAvailableAtUtc { get; set; }
    }
}
