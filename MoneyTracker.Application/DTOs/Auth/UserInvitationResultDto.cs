namespace MoneyTracker.Application.DTOs.Auth
{
    public class UserInvitationResultDto
    {
        public string Email { get; set; } = string.Empty;
        public string InviteUrl { get; set; } = string.Empty;
        public bool EmailSent { get; set; }
    }
}
