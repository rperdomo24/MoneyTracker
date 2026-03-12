using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class AccountsPanelStateService : IAccountsPanelStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKeyProvider;

        public AccountsPanelStateService(
            IFilterStorageService storage,
            ICurrentUserKeyProvider userKeyProvider)
        {
            _storage = storage;
            _userKeyProvider = userKeyProvider;
        }

        private string Key()
            => _userKeyProvider.GetUserKey();

        public Task SaveAsync(bool isOpen)
            => _storage.SaveAsync(Key(), isOpen);

        public async Task<bool> GetAsync(bool defaultValue = false)
        {
            var stored = await _storage.GetAsync<bool?>(Key());
            return stored ?? defaultValue;
        }

        public Task ClearAsync()
            => _storage.RemoveAsync(Key());
    }
}
