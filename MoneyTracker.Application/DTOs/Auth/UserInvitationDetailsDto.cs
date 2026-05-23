namespace MoneyTracker.Application.DTOs.Auth
{
    public class UserInvitationDetailsDto
    {
        public int InvitationId { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
