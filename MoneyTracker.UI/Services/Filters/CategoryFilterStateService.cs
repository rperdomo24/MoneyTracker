using MoneyTracker.Application.DTOs;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class CategoryFilterStateService : ICategoryFilterStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKey;

        public CategoryFilterStateService(IFilterStorageService storage, ICurrentUserKeyProvider userKey)
        {
            _storage = storage;
            _userKey = userKey;
        }

        private string Key => StorageKeys.Filters.Categories(_userKey.GetUserKey());

        public Task SaveAsync(CategoryViewStateDto state)
            => _storage.SaveAsync(Key, state);

        public Task<CategoryViewStateDto?> GetAsync()
            => _storage.GetAsync<CategoryViewStateDto>(Key);

        public Task ClearAsync()
            => _storage.RemoveAsync(Key);
    }
}
