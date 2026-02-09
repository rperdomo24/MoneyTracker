using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class TransactionFilterStorage : ITransactionFilterStorage
    {
        private readonly ProtectedSessionStorage _session;
        private readonly ICurrentUserKeyProvider _userKeyProvider;

        public TransactionFilterStorage(
            ProtectedSessionStorage session,
            ICurrentUserKeyProvider userKeyProvider)
        {
            _session = session;
            _userKeyProvider = userKeyProvider;
        }

        private string Key(int accountId)
            => StorageKeys.Filters.AccountTransactions(_userKeyProvider.GetUserKey(), accountId);

        public async Task SaveAccountFilterAsync(int accountId, TransactionFilterDto filter)
            => await _session.SetAsync(Key(accountId), filter);

        public async Task<TransactionFilterDto?> GetAccountFilterAsync(int accountId)
        {
            var result = await _session.GetAsync<TransactionFilterDto>(Key(accountId));
            return result.Success ? result.Value : null;
        }

        public async Task ClearAccountFilterAsync(int accountId)
            => await _session.DeleteAsync(Key(accountId));
    }
}
