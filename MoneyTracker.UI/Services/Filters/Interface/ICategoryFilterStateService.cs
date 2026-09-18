using MoneyTracker.Application.DTOs;

namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface ICategoryFilterStateService
    {
        Task SaveAsync(CategoryViewStateDto state);
        Task<CategoryViewStateDto?> GetAsync();
        Task ClearAsync();
    }
}
