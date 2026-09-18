using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IMerchantRepository
    {
        Task<List<Merchant>> GetAllAsync();
        Task<Merchant?> GetByIdAsync(int id);
        Task AddAsync(Merchant merchant);
        Task UpdateAsync(Merchant merchant);
        Task SoftDeleteAsync(int id);
        Task<bool> ExistsAsync(string name, int? excludeId = null);
        Task<Dictionary<int, int>> GetTransactionCountsAsync();
    }
}
