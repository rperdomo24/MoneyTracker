namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface IAccountsPanelStateService
    {
        Task SaveAsync(bool isOpen);
        Task<bool> GetAsync(bool defaultValue = false);
        Task ClearAsync();
    }
}
