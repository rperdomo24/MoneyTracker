using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class TransactionFilterStateService : ITransactionFilterStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKeyProvider;

        public TransactionFilterStateService(
            IFilterStorageService storage,
            ICurrentUserKeyProvider userKeyProvider)
        {
            _storage = storage;
            _userKeyProvider = userKeyProvider;
        }

        private string Key()
            => StorageKeys.Filters.Transactions(_userKeyProvider.GetUserKey());

        public Task SaveAsync(TransactionFilterDto filter)
            => _storage.SaveAsync(Key(), filter);

        public Task<TransactionFilterDto?> GetAsync()
            => _storage.GetAsync<TransactionFilterDto>(Key());

        public Task ClearAsync()
            => _storage.RemoveAsync(Key());
    }
}
