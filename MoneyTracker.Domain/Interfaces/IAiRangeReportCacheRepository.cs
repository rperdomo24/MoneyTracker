using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IAiRangeReportCacheRepository
    {
        Task<AiRangeReportCache?> GetAsync(DateOnly from, DateOnly to);
        Task UpsertAsync(DateOnly from, DateOnly to, string content);
    }
}
