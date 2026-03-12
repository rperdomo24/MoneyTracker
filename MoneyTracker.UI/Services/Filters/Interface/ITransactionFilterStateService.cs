using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface ITransactionFilterStateService
    {
        Task SaveAsync(TransactionFilterDto filter);
        Task<TransactionFilterDto?> GetAsync();
        Task ClearAsync();
    }
}
