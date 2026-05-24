namespace MoneyTracker.Application.DTOs
{
    public class CategoryViewStateDto
    {
        public int ViewMode { get; set; } = 0;
        public string? SearchText { get; set; }
    }
}
