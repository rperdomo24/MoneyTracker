namespace MoneyTracker.Application.Constants.Configuration
{
    public class GmailSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>Optional override. When empty, the redirect URI is derived from ApplicationSettings.PublicBaseUrl.</summary>
        public string? RedirectUri { get; set; }

        public int MaxMessagesPerSync { get; set; } = 25;
    }
}
