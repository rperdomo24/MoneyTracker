using MoneyTracker.Domain.Enums.Category;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }

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

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } 
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
