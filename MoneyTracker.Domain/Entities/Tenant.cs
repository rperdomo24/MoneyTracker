using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class Tenant
    {
        [Key]
        public Guid TenantId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid OwnerUserId { get; set; }
    }
}
