using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class Merchant : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
