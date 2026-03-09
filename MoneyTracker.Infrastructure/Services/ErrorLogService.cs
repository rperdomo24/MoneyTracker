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

        public ErrorLogService(MoneyTrackerDbContext dbContext, ILogger<ErrorLogService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
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
