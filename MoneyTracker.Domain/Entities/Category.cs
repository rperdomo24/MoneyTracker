using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class Category : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public CategoryTypeEnum Type { get; set; } = CategoryTypeEnum.Expense;

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }

        public bool IsSystem { get; set; } = false;
        [MaxLength(64)]
        public string? SystemCategoryCode { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
