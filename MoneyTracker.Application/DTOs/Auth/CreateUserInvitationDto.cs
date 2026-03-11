namespace MoneyTracker.Application.DTOs.Auth
{
    public class CreateUserInvitationDto
    {
        public string Email { get; set; } = string.Empty;
        public string InviteUrlTemplate { get; set; } = string.Empty;
    }
}
