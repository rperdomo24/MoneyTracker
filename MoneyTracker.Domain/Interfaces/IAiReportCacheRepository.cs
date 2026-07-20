using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IAiReportCacheRepository
    {
        Task<AiReportCache?> GetAsync(int year, int month);
        Task UpsertAsync(int year, int month, string content);
    }
}
