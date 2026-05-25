using MoneyTracker.Application.DTOs.Search;

namespace MoneyTracker.Application.Interfaces
{
    public interface IGlobalSearchRepository
    {
        Task<GlobalSearchResultDto> SearchAsync(string query, int maxPerType = 5);
    }
}
