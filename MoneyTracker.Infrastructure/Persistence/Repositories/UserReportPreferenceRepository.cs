using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class UserReportPreferenceRepository : IUserReportPreferenceRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UserReportPreferenceRepository> _logger;

        public UserReportPreferenceRepository(IServiceScopeFactory scopeFactory, ILogger<UserReportPreferenceRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<UserReportPreference?> GetAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                return await context.UserReportPreferences.AsNoTracking().FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading user report preference");
                return null;
            }
        }

        public async Task SaveAsync(UserReportPreference preference)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
                var existing = await context.UserReportPreferences.FirstOrDefaultAsync();

                if (existing is not null)
                {
                    existing.Year = preference.Year;
                    existing.Month = preference.Month;
                    existing.CategoriesJson = preference.CategoriesJson;
                    existing.ActiveCardFilter = preference.ActiveCardFilter;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    preference.UpdatedAt = DateTime.UtcNow;
                    context.UserReportPreferences.Add(preference);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user report preference");
            }
        }
    }
}
