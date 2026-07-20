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
            try
            {
                if (value is null)
                {
                    await _session.DeleteAsync(key);
                    return;
                }
                await _session.SetAsync(key, value);
            }
            catch (InvalidOperationException) { }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            try
            {
                var result = await _session.GetAsync<T>(key);
                return result.Success ? result.Value : default;
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Security.Cryptography.CryptographicException)
            {
                try { await _session.DeleteAsync(key); } catch { }
                return default;
            }
        }

        public async Task RemoveAsync(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            try
            {
                await _session.DeleteAsync(key);
            }
            catch (InvalidOperationException) { }
        }
    }
}
