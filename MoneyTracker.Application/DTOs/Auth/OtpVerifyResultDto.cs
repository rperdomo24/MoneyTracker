namespace MoneyTracker.Application.DTOs.Auth
{
    public class OtpVerifyResultDto
    {
        public bool SignedIn { get; init; }
        public bool SessionExpired { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
