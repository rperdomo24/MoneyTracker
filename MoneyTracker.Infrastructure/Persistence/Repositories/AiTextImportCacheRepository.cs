using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class AiTextImportCacheRepository : IAiTextImportCacheRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiTextImportCacheRepository> _logger;

        public AiTextImportCacheRepository(IServiceScopeFactory scopeFactory, ILogger<AiTextImportCacheRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<AiTextImportCache?> GetByHashAsync(string inputHash)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.AiTextImportCaches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.InputHash == inputHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading text import cache for hash {Hash}", inputHash);
                return null;
            }
        }

        public async Task SaveAsync(string inputHash, string content)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                var existing = await context.AiTextImportCaches
                    .FirstOrDefaultAsync(c => c.InputHash == inputHash);

                if (existing is not null)
                {
                    existing.Content = content;
                    existing.GeneratedAt = DateTime.UtcNow;
                }
                else
                {
                    context.AiTextImportCaches.Add(new AiTextImportCache
                    {
                        InputHash = inputHash,
                        Content = content,
                        GeneratedAt = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving text import cache for hash {Hash}", inputHash);
            }
        }
    }
}
