namespace MoneyTracker.Application.DTOs
{
    public class UserProfileSettingsDto
    {
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string TenantId { get; set; } = "-";
        public bool TwoFactorEnabled { get; set; }
        public string? AvatarDataUrl { get; set; }
    }
}
