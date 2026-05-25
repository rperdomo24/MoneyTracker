using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Search;

namespace MoneyTracker.Application.Interfaces
{
    public interface IGlobalSearchService
    {
        Task<OperationResult<GlobalSearchResultDto>> SearchAsync(string query);
    }
}
