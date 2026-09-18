using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Calendar;
using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.Application.Interfaces
{
    public interface ICalendarService
    {
        Task<OperationResult<Dictionary<int, List<CalendarEventDto>>>> GetMonthEventsAsync(int year, int month);
        Task<OperationResult<Dictionary<int, (decimal Income, decimal Expense)>>> GetMonthDayTotalsAsync(int year, int month);
        Task<OperationResult<TransactionSummaryDto>> GetDayBalanceAsync(DateOnly date);
    }
}
