using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IAiTextImportCacheRepository
    {
        Task<AiTextImportCache?> GetByHashAsync(string inputHash);
        Task SaveAsync(string inputHash, string content);
    }
}
