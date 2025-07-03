using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Application.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public CategoryTypeEnum Type { get; set; }
        public CategoryIcon Icon { get; set; }
        public string? Color { get; set; }
        public bool IsSystem { get; set; }
        public DateTime UpdatedAt { get; set; }
        public CategoryDto? Parent { get; set; }
        public List<CategoryDto> Children { get; set; } = new();

        // View Model properties
        public bool IsExpanded { get; set; }
    }
}
