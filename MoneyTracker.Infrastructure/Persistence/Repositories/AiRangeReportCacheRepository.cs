using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class AiRangeReportCacheRepository : IAiRangeReportCacheRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiRangeReportCacheRepository> _logger;

        public AiRangeReportCacheRepository(IServiceScopeFactory scopeFactory, ILogger<AiRangeReportCacheRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<AiRangeReportCache?> GetAsync(DateOnly from, DateOnly to)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.AiRangeReportCaches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.FromDate == from && c.ToDate == to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading AI range report cache for {From}-{To}", from, to);
                return null;
            }
        }

        public async Task UpsertAsync(DateOnly from, DateOnly to, string content)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                var existing = await context.AiRangeReportCaches
                    .FirstOrDefaultAsync(c => c.FromDate == from && c.ToDate == to);

                if (existing is not null)
                {
                    existing.Content = content;
                    existing.GeneratedAt = DateTime.UtcNow;
                }
                else
                {
                    context.AiRangeReportCaches.Add(new AiRangeReportCache
                    {
                        FromDate = from,
                        ToDate = to,
                        Content = content,
                        GeneratedAt = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AI range report cache for {From}-{To}", from, to);
            }
        }
    }
}
