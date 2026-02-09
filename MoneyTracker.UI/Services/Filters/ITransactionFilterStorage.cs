using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.UI.Services.Filters
{
    public interface ITransactionFilterStorage
    {
        Task SaveAccountFilterAsync(int accountId, TransactionFilterDto filter);
        Task<TransactionFilterDto?> GetAccountFilterAsync(int accountId);
        Task ClearAccountFilterAsync(int accountId);
    }
}
