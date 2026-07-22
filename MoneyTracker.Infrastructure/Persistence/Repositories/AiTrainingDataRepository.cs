using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Ai;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class AiTrainingDataRepository : IAiTrainingDataRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiTrainingDataRepository> _logger;

        public AiTrainingDataRepository(IServiceScopeFactory scopeFactory, ILogger<AiTrainingDataRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<int> AddAsync(AiTrainingData data)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                context.AiTrainingData.Add(data);
                await context.SaveChangesAsync();
                return data.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AI training data for service {Service}", data.ServiceType);
                return 0;
            }
        }

        public async Task UpdateFeedbackAsync(int id, AiUserFeedback feedback)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                var record = await context.AiTrainingData.FirstOrDefaultAsync(r => r.Id == id);
                if (record is null) return;
                record.UserFeedback = feedback;
                record.FeedbackAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AI training feedback for record {Id}", id);
            }
        }
    }
}
