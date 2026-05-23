using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class UserInvitation
    {
        public int Id { get; set; }

        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(256)]
        public string NormalizedEmail { get; set; } = string.Empty;

        [MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        public Guid? InvitedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? LastSentAtUtc { get; set; }

        public DateTime? AcceptedAtUtc { get; set; }
    }
}
