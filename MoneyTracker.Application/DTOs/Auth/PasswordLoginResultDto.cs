namespace MoneyTracker.Application.DTOs.Auth
{
    public class PasswordLoginResultDto
    {
        public bool SignedIn { get; init; }
        public bool RequiresOtp { get; init; }
        public bool EmailNotConfirmed { get; init; }
        public string? ErrorMessage { get; init; }
        public string? EmailForPendingPage { get; init; }
        public DateTime? OtpExpiresAtUtc { get; init; }
    }
}
