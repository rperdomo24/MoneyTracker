using Microsoft.AspNetCore.Identity;
using MoneyTracker.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Infrastructure.Persistence
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public Guid TenantId { get; set; }

        [MaxLength(100)]
        public string? DisplayName { get; set; }

        public UserAvatar? Avatar { get; set; }
    }
}
