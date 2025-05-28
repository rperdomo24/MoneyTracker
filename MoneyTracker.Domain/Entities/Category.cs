using MoneyTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }
        public CategoryType Type { get; set; }
        public Category? Parent { get; set; }

        public ICollection<Category> Subcategories { get; set; } = new List<Category>();

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }
    }

}
