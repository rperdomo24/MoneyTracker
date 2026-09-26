using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class GmailConnection : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string EncryptedRefreshToken { get; set; } = string.Empty;

        public DateTime? LastSyncAtUtc { get; set; }
        public bool AutoSyncEnabled { get; set; } = false;
        public int SyncIntervalMinutes { get; set; } = 30;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
