using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class AccountFilterStateService : IAccountFilterStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKeyProvider;

        public AccountFilterStateService(
            IFilterStorageService storage,
            ICurrentUserKeyProvider userKeyProvider)
        {
            _storage = storage;
            _userKeyProvider = userKeyProvider;
        }

        private string Key(int accountId)
            => StorageKeys.Filters.AccountTransactions(_userKeyProvider.GetUserKey(), accountId);

        public Task SaveAsync(int accountId, TransactionFilterDto filter)
            => _storage.SaveAsync(Key(accountId), filter);

        public Task<TransactionFilterDto?> GetAsync(int accountId)
            => _storage.GetAsync<TransactionFilterDto>(Key(accountId));

        public Task ClearAsync(int accountId)
            => _storage.RemoveAsync(Key(accountId));
    }
}
