using MoneyTracker.UI.Models.Filters;

namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface IRecurringTransactionFilterStateService
    {
        Task SaveAsync(RecurringTransactionFilter filter);
        Task<RecurringTransactionFilter> GetAsync();
        Task ClearAsync();
    }
}
