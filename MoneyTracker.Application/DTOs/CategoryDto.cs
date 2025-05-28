namespace MoneyTracker.Application.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }

        public List<CategoryDto> Subcategories { get; set; } = new();
    }
}
