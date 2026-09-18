using MoneyTracker.Application.DTOs.Reports;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class RangeReportStateService : IRangeReportStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKey;

        public RangeReportStateService(IFilterStorageService storage, ICurrentUserKeyProvider userKey)
        {
            _storage = storage;
            _userKey = userKey;
        }

        private string Key => StorageKeys.Filters.RangeReport(_userKey.GetUserKey());

        public Task SaveAsync(RangeReportStateDto state) => _storage.SaveAsync(Key, state);
        public Task<RangeReportStateDto?> GetAsync() => _storage.GetAsync<RangeReportStateDto>(Key);
        public Task ClearAsync() => _storage.RemoveAsync(Key);
    }
}
