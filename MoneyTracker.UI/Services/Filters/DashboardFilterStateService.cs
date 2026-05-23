using MoneyTracker.Application.DTOs.Dashboard;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class DashboardFilterStateService : IDashboardFilterStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKey;

        public DashboardFilterStateService(
            IFilterStorageService storage,
            ICurrentUserKeyProvider userKey)
        {
            _storage = storage;
            _userKey = userKey;
        }

        private string Key => StorageKeys.Filters.Dashboard(_userKey.GetUserKey());

        public Task SaveAsync(DashboardFilterDto filter)
            => _storage.SaveAsync(Key, filter);

        public Task<DashboardFilterDto?> GetAsync()
            => _storage.GetAsync<DashboardFilterDto>(Key);

        public Task ClearAsync()
            => _storage.RemoveAsync(Key);
    }
}
