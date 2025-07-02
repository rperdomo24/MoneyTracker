using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Application.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public CategoryIcon Icon { get; set; }
        public string? Color { get; set; }
        public CategoryType Type { get; set; }
        public CategoryDto? Parent { get; set; }
        public List<CategoryDto> Subcategories { get; set; } = new();
        public bool IsExpanded { get; set; }
    }
}
