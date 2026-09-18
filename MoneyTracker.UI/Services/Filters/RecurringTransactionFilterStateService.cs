using MoneyTracker.UI.Models.Filters;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class RecurringTransactionFilterStateService : IRecurringTransactionFilterStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKey;

        public RecurringTransactionFilterStateService(IFilterStorageService storage, ICurrentUserKeyProvider userKey)
        {
            _storage = storage;
            _userKey = userKey;
        }

        private string Key => StorageKeys.Filters.RecurringTransactions(_userKey.GetUserKey());

        public Task SaveAsync(RecurringTransactionFilter filter)
            => _storage.SaveAsync(Key, filter);

        public async Task<RecurringTransactionFilter> GetAsync()
        {
            var value = await _storage.GetAsync<RecurringTransactionFilter?>(Key);
            return value ?? RecurringTransactionFilter.All;
        }

        public Task ClearAsync()
            => _storage.RemoveAsync(Key);
    }
}
