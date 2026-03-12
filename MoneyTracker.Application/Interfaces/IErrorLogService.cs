using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Interfaces
{
    public interface IErrorLogService
    {
        Task LogAsync(ExceptionLogEntryDto entry, CancellationToken cancellationToken = default);
        Task LogExceptionAsync(Exception exception, string? customMessage = null, string level = "Error", CancellationToken cancellationToken = default);
        Task LogMessageAsync(string message, string level = "Warning", string exceptionType = "HandledOperation", CancellationToken cancellationToken = default);
    }
}
