using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class AiCallLogRepository : IAiCallLogRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiCallLogRepository> _logger;

        public AiCallLogRepository(IServiceScopeFactory scopeFactory, ILogger<AiCallLogRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task AddAsync(AiCallLog log)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                context.AiCallLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AI call log for service {Service}", log.Service);
            }
        }
    }
}
