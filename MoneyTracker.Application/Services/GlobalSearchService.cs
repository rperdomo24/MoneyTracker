using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Search;
using MoneyTracker.Application.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class GlobalSearchService : IGlobalSearchService
    {
        private readonly IGlobalSearchRepository _repository;
        private readonly ILogger<GlobalSearchService> _logger;

        public GlobalSearchService(IGlobalSearchRepository repository, ILogger<GlobalSearchService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OperationResult<GlobalSearchResultDto>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                return OperationResult<GlobalSearchResultDto>.Fail("Enter at least 2 characters to search.");

            var result = await _repository.SearchAsync(query.Trim());
            return OperationResult<GlobalSearchResultDto>.Ok(result);
        }
    }
}
