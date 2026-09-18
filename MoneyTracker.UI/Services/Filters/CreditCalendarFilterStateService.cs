using MoneyTracker.UI.Models.Filters;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class CreditCalendarFilterStateService : ICreditCalendarFilterStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKey;

        public CreditCalendarFilterStateService(IFilterStorageService storage, ICurrentUserKeyProvider userKey)
        {
            _storage = storage;
            _userKey = userKey;
        }

        private string Key => StorageKeys.Filters.CreditCalendar(_userKey.GetUserKey());

        public Task SaveAsync(CreditCalendarFilter filter)
            => _storage.SaveAsync(Key, filter);

        public async Task<CreditCalendarFilter> GetAsync()
        {
            var value = await _storage.GetAsync<CreditCalendarFilter?>(Key);
            return value ?? CreditCalendarFilter.All;
        }

        public Task ClearAsync()
            => _storage.RemoveAsync(Key);
    }
}
