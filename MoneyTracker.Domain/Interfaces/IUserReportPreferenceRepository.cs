using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IUserReportPreferenceRepository
    {
        Task<UserReportPreference?> GetAsync();
        Task SaveAsync(UserReportPreference preference);
    }
}
