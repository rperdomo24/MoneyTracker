using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MoneyTracker.UI.Services.Filters.Interface;

namespace MoneyTracker.UI.Services.Filters
{
    public class FilterStorageService : IFilterStorageService
    {
        private readonly ProtectedSessionStorage _session;

        public FilterStorageService(ProtectedSessionStorage session)
        {
            _session = session;
        }

        public async Task SaveAsync<T>(string key, T value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (value is null)
            {
                await _session.DeleteAsync(key);
                return;
            }

            await _session.SetAsync(key, value);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            var result = await _session.GetAsync<T>(key);
            return result.Success ? result.Value : default;
        }

        public async Task RemoveAsync(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            await _session.DeleteAsync(key);
        }
    }
}
