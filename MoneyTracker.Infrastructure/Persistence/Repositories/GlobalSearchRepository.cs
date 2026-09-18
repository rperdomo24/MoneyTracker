using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.DTOs.Search;
using MoneyTracker.Application.Interfaces;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class GlobalSearchRepository : IGlobalSearchRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GlobalSearchRepository> _logger;

        private const float SimilarityThreshold = 0.1f;

        public GlobalSearchRepository(IServiceScopeFactory scopeFactory, ILogger<GlobalSearchRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<GlobalSearchResultDto> SearchAsync(string query, int maxPerType = 5)
        {
            var result = new GlobalSearchResultDto();
            var q = query.ToLower().Trim();
            if (string.IsNullOrEmpty(q)) return result;

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                // Sequential — DbContext is NOT thread-safe
                result.Transactions = await SearchTransactionsAsync(context, q, maxPerType);
                result.Accounts = await SearchAccountsAsync(context, q, maxPerType);
                result.Categories = await SearchCategoriesAsync(context, q, maxPerType);

                _logger.LogDebug(
                    "Global search '{Query}': {T}tx {A}acc {C}cat",
                    query, result.Transactions.Count, result.Accounts.Count, result.Categories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Global search failed. Query: {Query}", query);
                throw;
            }

            return result;
        }

        private static async Task<List<TransactionSearchItemDto>> SearchTransactionsAsync(
            MoneyTrackerDbContext context, string q, int max)
        {
            return await context.Transaction
                .AsNoTracking()
                .Where(t => !t.IsDeleted
                    && (EF.Functions.TrigramsSimilarity(t.Name.ToLower(), q) > SimilarityThreshold
                        || t.Name.ToLower().Contains(q)
                        || (t.Description != null && t.Description.ToLower().Contains(q))))
                .OrderByDescending(t => EF.Functions.TrigramsSimilarity(t.Name.ToLower(), q))
                .Take(max)
                .Select(t => new TransactionSearchItemDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Amount = t.Amount,
                    Date = t.Date,
                    AccountId = t.AccountId,
                    AccountName = t.Account != null ? t.Account.Name : string.Empty,
                    CategoryName = t.Category != null ? t.Category.Name : null
                })
                .ToListAsync();
        }

        private static async Task<List<AccountSearchItemDto>> SearchAccountsAsync(
            MoneyTrackerDbContext context, string q, int max)
        {
            return await context.Accounts
                .AsNoTracking()
                .Where(a => !a.IsDeleted
                    && (EF.Functions.TrigramsSimilarity(a.Name.ToLower(), q) > SimilarityThreshold
                        || a.Name.ToLower().Contains(q)
                        || (a.BankName != null && a.BankName.ToLower().Contains(q))
                        || (a.CardDisplayName != null && a.CardDisplayName.ToLower().Contains(q))))
                .OrderByDescending(a => EF.Functions.TrigramsSimilarity(a.Name.ToLower(), q))
                .Take(max)
                .Select(a => new AccountSearchItemDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Balance = a.Balance,
                    BankName = a.BankName
                })
                .ToListAsync();
        }

        private static async Task<List<CategorySearchItemDto>> SearchCategoriesAsync(
            MoneyTrackerDbContext context, string q, int max)
        {
            return await context.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted
                    && (EF.Functions.TrigramsSimilarity(c.Name.ToLower(), q) > SimilarityThreshold
                        || c.Name.ToLower().Contains(q)))
                .OrderByDescending(c => EF.Functions.TrigramsSimilarity(c.Name.ToLower(), q))
                .Take(max)
                .Select(c => new CategorySearchItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    ParentId = c.ParentId
                })
                .ToListAsync();
        }
    }
}
