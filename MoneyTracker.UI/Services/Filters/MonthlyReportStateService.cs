using MoneyTracker.Application.DTOs.Reports;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Const;

namespace MoneyTracker.UI.Services.Filters
{
    public class MonthlyReportStateService : IMonthlyReportStateService
    {
        private readonly IFilterStorageService _storage;
        private readonly ICurrentUserKeyProvider _userKey;

        public MonthlyReportStateService(IFilterStorageService storage, ICurrentUserKeyProvider userKey)
        {
            _storage = storage;
            _userKey = userKey;
        }

        private string Key => StorageKeys.Filters.MonthlyReport(_userKey.GetUserKey());

        public Task SaveAsync(MonthlyReportStateDto state) => _storage.SaveAsync(Key, state);
        public Task<MonthlyReportStateDto?> GetAsync() => _storage.GetAsync<MonthlyReportStateDto>(Key);
        public Task ClearAsync() => _storage.RemoveAsync(Key);
    }
}
