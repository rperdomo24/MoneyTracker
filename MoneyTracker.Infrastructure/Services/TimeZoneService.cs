using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MoneyTracker.Infrastructure.Services
{
    public class TimeZoneService : ITimeZoneService
    {
        private readonly TimeZoneInfo _timeZone;
        private readonly ILogger<TimeZoneService> _logger;
        private readonly string _timeZoneId;

        public TimeZoneService(IOptions<ApplicationSettings> options, ILogger<TimeZoneService> logger)
        {
            _logger = logger;
            _timeZoneId = options.Value.DefaultTimeZone;

            try
            {
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                _logger.LogInformation("TimeZoneService initialized with {TimeZone} (UTC{Offset}).",
                    _timeZone.DisplayName,
                    _timeZone.BaseUtcOffset);
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogWarning("Timezone '{TimeZoneId}' was not found. Falling back to UTC.", _timeZoneId);
                _timeZone = TimeZoneInfo.Utc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring timezone '{TimeZoneId}'. Falling back to UTC.", _timeZoneId);
                _timeZone = TimeZoneInfo.Utc;
            }
        }

        public DateTime ConvertToUtc(DateTime localDate)
        {
            try
            {
                if (localDate.Kind == DateTimeKind.Utc)
                    return localDate;

                var dateWithCorrectKind = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
                return TimeZoneInfo.ConvertTimeToUtc(dateWithCorrectKind, _timeZone);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting local time to UTC. Using fallback conversion.");
                return localDate.Kind == DateTimeKind.Utc ? localDate : localDate.ToUniversalTime();
            }
        }

        public DateTime? ConvertToUtc(DateTime? localDate)
        {
            if (!localDate.HasValue)
                return null;

            try
            {
                return ConvertToUtc(localDate.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting nullable local date to UTC.");
                return localDate.Value.ToUniversalTime();
            }
        }

        public DateTime ConvertFromUtc(DateTime utcDate)
        {
            try
            {
                // Npgsql often returns timestamp values as Unspecified; treat them as UTC.
                if (utcDate.Kind == DateTimeKind.Unspecified)
                {
                    utcDate = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
                }
                else if (utcDate.Kind == DateTimeKind.Local)
                {
                    _logger.LogWarning("Local DateTime provided for UTC conversion. Normalizing from {DateKind}.", utcDate.Kind);
                    utcDate = utcDate.ToUniversalTime();
                }

                return TimeZoneInfo.ConvertTimeFromUtc(utcDate, _timeZone);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting UTC to local time. Returning original value.");
                return utcDate;
            }
        }

        public DateTime? ConvertFromUtc(DateTime? utcDate)
        {
            if (!utcDate.HasValue)
                return null;

            try
            {
                return ConvertFromUtc(utcDate.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting nullable UTC date to local time.");
                return utcDate.Value;
            }
        }

        public DateTime GetNowInUtc()
        {
            try
            {
                var localNow = TimeZoneInfo.ConvertTime(DateTime.Now, _timeZone);
                return TimeZoneInfo.ConvertTimeToUtc(localNow, _timeZone);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting current time in UTC. Using DateTime.UtcNow.");
                return DateTime.UtcNow;
            }
        }

        public DateTime GetLocalTimeInConfiguredTimeZone()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting local time in configured timezone. Using DateTime.Now.");
                return DateTime.Now;
            }
        }
    }
}
