namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface IFilterStorageService
    {
        Task SaveAsync<T>(string key, T value);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
    }
}
