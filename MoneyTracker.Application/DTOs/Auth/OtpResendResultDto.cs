namespace MoneyTracker.Application.DTOs.Auth
{
    public class OtpResendResultDto
    {
        public bool SessionExpired { get; init; }
        public DateTime? OtpExpiresAtUtc { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
