using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IAiCallLogRepository
    {
        Task AddAsync(AiCallLog log);
    }
}
