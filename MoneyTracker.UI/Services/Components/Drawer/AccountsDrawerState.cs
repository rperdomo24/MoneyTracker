using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace MoneyTracker.UI.Services.Components.Drawer
{
    public class AccountsDrawerState
    {
        private const string StorageKey = "ui.accountsDrawer.isOpen";
        private readonly ProtectedSessionStorage _sessionStorage;

        public AccountsDrawerState(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        public bool IsOpen { get; private set; }

        // UI state changed (open/close)
        public event Action? OnChange;

        // Data refresh requested (transactions/accounts changed)
        public event Action? OnRefreshRequested;

        public async Task InitializeAsync()
        {
            var result = await _sessionStorage.GetAsync<bool>(StorageKey);
            IsOpen = result.Success && result.Value;
            OnChange?.Invoke();
        }

        public async Task OpenAsync()
        {
            IsOpen = true;
            await _sessionStorage.SetAsync(StorageKey, true);
            OnChange?.Invoke();
        }

        public async Task CloseAsync()
        {
            IsOpen = false;
            await _sessionStorage.SetAsync(StorageKey, false);
            OnChange?.Invoke();
        }

        public async Task ToggleAsync()
        {
            if (IsOpen) await CloseAsync();
            else await OpenAsync();
        }

        // Call this when transactions/accounts are created/updated/deleted
        public void RequestRefresh()
        {
            OnRefreshRequested?.Invoke();
        }
    }
}