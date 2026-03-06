using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class TransactionAttachment : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        public int TransactionId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        public int FileSize { get; set; }

        // This field is optional, as not all attachments may have a content type
        [MaxLength(100)]
        public string? ContentType { get; set; }

        [Required]
        public byte[] FileContent { get; set; } = Array.Empty<byte>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Transaction Transaction { get; set; } = null!;
    }
}
