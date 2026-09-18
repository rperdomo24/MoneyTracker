using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class AiReportCacheRepository : IAiReportCacheRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiReportCacheRepository> _logger;

        public AiReportCacheRepository(IServiceScopeFactory scopeFactory, ILogger<AiReportCacheRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<AiReportCache?> GetAsync(int year, int month)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.AiReportCaches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Year == year && c.Month == month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading AI report cache for {Year}-{Month}", year, month);
                return null;
            }
        }

        public async Task UpsertAsync(int year, int month, string content)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                var existing = await context.AiReportCaches
                    .FirstOrDefaultAsync(c => c.Year == year && c.Month == month);

                if (existing is not null)
                {
                    existing.Content = content;
                    existing.GeneratedAt = DateTime.UtcNow;
                }
                else
                {
                    context.AiReportCaches.Add(new AiReportCache
                    {
                        Year = year,
                        Month = month,
                        Content = content,
                        GeneratedAt = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AI report cache for {Year}-{Month}", year, month);
            }
        }
    }
}
