using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class UserAvatar : ITenantOwned
    {
        [Key]
        public Guid UserId { get; set; }

        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(20)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public byte[] Content { get; set; } = Array.Empty<byte>();

        public int SizeBytes { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
