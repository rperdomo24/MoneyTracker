using MoneyTracker.UI.Models.Filters;

namespace MoneyTracker.UI.Services.Filters.Interface
{
    public interface ICreditCalendarFilterStateService
    {
        Task SaveAsync(CreditCalendarFilter filter);
        Task<CreditCalendarFilter> GetAsync();
        Task ClearAsync();
    }
}
