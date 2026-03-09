using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Interfaces
{
    public interface IErrorLogService
    {
        Task LogAsync(ExceptionLogEntryDto entry, CancellationToken cancellationToken = default);
    }
}
