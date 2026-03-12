namespace MoneyTracker.Application.DTOs.Auth
{
    public class AcceptInvitationRequestDto
    {
        public string Token { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
