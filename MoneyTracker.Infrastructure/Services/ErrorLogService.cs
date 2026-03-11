using Microsoft.Extensions.Logging;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Services
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly MoneyTrackerDbContext _dbContext;
        private readonly ILogger<ErrorLogService> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ITenantContext _tenantContext;

        public ErrorLogService(
            MoneyTrackerDbContext dbContext,
            ILogger<ErrorLogService> logger,
            ICurrentUserService currentUserService,
            ITenantContext tenantContext)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _tenantContext = tenantContext;
        }

        public async Task LogAsync(ExceptionLogEntryDto entry, CancellationToken cancellationToken = default)
        {
            var log = new ErrorLog
            {
                CreatedAtUtc = entry.CreatedAtUtc,
                Level = Truncate(entry.Level, 32, "Error"),
                ExceptionType = Truncate(entry.ExceptionType, 512, "Exception"),
                Message = Truncate(entry.Message, 4000, "Unhandled exception."),
                StackTrace = entry.StackTrace,
                InnerException = entry.InnerException,
                HttpMethod = Truncate(entry.HttpMethod, 16),
                Path = Truncate(entry.Path, 1024),
                QueryString = Truncate(entry.QueryString, 2048),
                TraceId = Truncate(entry.TraceId, 128),
                UserId = entry.UserId,
                TenantId = entry.TenantId,
                Environment = Truncate(entry.Environment, 128)
            };

            try
            {
                _dbContext.Set<ErrorLog>().Add(log);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist unhandled exception log.");
            }
        }

        public Task LogExceptionAsync(Exception exception, string? customMessage = null, string level = "Error", CancellationToken cancellationToken = default)
        {
            return LogAsync(CreateEntry(
                customMessage ?? exception.Message,
                level,
                exception.GetType().FullName ?? exception.GetType().Name,
                exception.StackTrace,
                exception.InnerException?.ToString()), cancellationToken);
        }

        public Task LogMessageAsync(string message, string level = "Warning", string exceptionType = "HandledOperation", CancellationToken cancellationToken = default)
        {
            return LogAsync(CreateEntry(message, level, exceptionType, null, null), cancellationToken);
        }

        private ExceptionLogEntryDto CreateEntry(
            string message,
            string level,
            string exceptionType,
            string? stackTrace,
            string? innerException)
        {
            return new ExceptionLogEntryDto
            {
                CreatedAtUtc = DateTime.UtcNow,
                Level = level,
                ExceptionType = exceptionType,
                Message = message,
                StackTrace = stackTrace,
                InnerException = innerException,
                UserId = _currentUserService.UserId,
                TenantId = _tenantContext.TenantId,
                Environment = null
            };
        }

        private static string Truncate(string? value, int maxLength, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
