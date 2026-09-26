namespace MoneyTracker.Application.DTOs.Gmail
{
    public class GmailConnectionDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime? LastSyncAtUtc { get; set; }
        public bool AutoSyncEnabled { get; set; }
        public int SyncIntervalMinutes { get; set; } = 30;
    }
}
