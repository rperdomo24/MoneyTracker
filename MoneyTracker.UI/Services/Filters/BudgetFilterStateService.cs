using MoneyTracker.Application.DTOs.Budgets;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class BudgetFilterStateService : IBudgetFilterStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKey;

        public BudgetFilterStateService(IFilterStorageService storage, ICurrentUserKeyProvider userKey)
        {
            _storage = storage;
            _userKey = userKey;
        }

        private string Key => StorageKeys.Filters.Budgets(_userKey.GetUserKey());

        public Task SaveAsync(BudgetFilterDto filter)
            => _storage.SaveAsync(Key, filter);

        public Task<BudgetFilterDto?> GetAsync()
            => _storage.GetAsync<BudgetFilterDto>(Key);

        public Task ClearAsync()
            => _storage.RemoveAsync(Key);
    }
}
