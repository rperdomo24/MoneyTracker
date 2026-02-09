using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface IAccountFilterStateService
    {
        Task SaveAsync(int accountId, TransactionFilterDto filter);
        Task<TransactionFilterDto?> GetAsync(int accountId);
        Task ClearAsync(int accountId);
    }
}
