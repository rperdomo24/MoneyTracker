using Microsoft.Extensions.Logging;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Services
{
    public class AuthAuditService : IAuthAuditService
    {
        private readonly MoneyTrackerDbContext _dbContext;
        private readonly ILogger<AuthAuditService> _logger;

        public AuthAuditService(
            MoneyTrackerDbContext dbContext,
            ILogger<AuthAuditService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task LogAsync(AuthAuditEntryDto entry, CancellationToken cancellationToken = default)
        {
            var auditLog = new AuthAuditLog
            {
                CreatedAtUtc = entry.CreatedAtUtc,
                Action = TruncateRequired(entry.Action, 64, "Unknown"),
                Outcome = TruncateRequired(entry.Outcome, 16, "Unknown"),
                FailureReason = Truncate(entry.FailureReason, 256),
                EmailMasked = Truncate(entry.EmailMasked, 128),
                UserId = entry.UserId,
                TenantId = entry.TenantId,
                IpAddress = Truncate(entry.IpAddress, 64),
                UserAgent = Truncate(entry.UserAgent, 1024),
                HttpMethod = Truncate(entry.HttpMethod, 16),
                Path = Truncate(entry.Path, 1024),
                TraceId = Truncate(entry.TraceId, 128)
            };

            try
            {
                _dbContext.Set<AuthAuditLog>().Add(auditLog);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist auth audit log.");
            }
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }

        private static string TruncateRequired(string? value, int maxLength, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
