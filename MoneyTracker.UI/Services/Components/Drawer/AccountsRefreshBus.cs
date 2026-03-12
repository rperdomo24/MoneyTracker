namespace MoneyTracker.UI.Services.Components.Drawer
{
    public class AccountsRefreshBus
    {
        public event Action? OnRefresh;

        public void Notify()
            => OnRefresh?.Invoke();
    }
}
