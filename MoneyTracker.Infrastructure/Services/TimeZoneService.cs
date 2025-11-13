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
                _logger.LogInformation("✅ TimeZoneService inicializado: {TimeZone} (UTC{Offset})",
                    _timeZone.DisplayName,
                    _timeZone.BaseUtcOffset);
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogWarning("⚠️ TimeZone '{TimeZoneId}' no encontrado, usando UTC como fallback", _timeZoneId);
                _timeZone = TimeZoneInfo.Utc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error configurando timezone '{TimeZoneId}'", _timeZoneId);
                _timeZone = TimeZoneInfo.Utc;
            }
        }

        public DateTime ConvertToUtc(DateTime localDate)
        {
            try
            {
                // Si ya es UTC, no convertir
                if (localDate.Kind == DateTimeKind.Utc)
                    return localDate;

                var dateWithCorrectKind = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
                var utcDate = TimeZoneInfo.ConvertTimeToUtc(dateWithCorrectKind, _timeZone);

                _logger.LogDebug("🕐 Convertido a UTC: {Local} -> {Utc}", localDate, utcDate);
                return utcDate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error convirtiendo hora local a UTC, usando fallback");
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
                _logger.LogWarning(ex, "⚠️ Error convirtiendo fecha nullable a UTC");
                return localDate.Value.ToUniversalTime();
            }
        }

        public DateTime ConvertFromUtc(DateTime utcDate)
        {
            try
            {
                // Asegurar que sea UTC
                if (utcDate.Kind != DateTimeKind.Utc)
                {
                    _logger.LogWarning("🔄 Se recibió fecha no-UTC para conversión, forzando UTC: {DateKind}", utcDate.Kind);
                    utcDate = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
                }

                var localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, _timeZone);

                _logger.LogDebug("🕐 Convertido desde UTC: {Utc} -> {Local}", utcDate, localDate);
                return localDate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error convirtiendo UTC a hora local, retornando UTC como fallback");
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
                _logger.LogWarning(ex, "⚠️ Error convirtiendo fecha nullable desde UTC");
                return utcDate.Value;
            }
        }

        public DateTime GetNowInUtc()
        {
            try
            {
                var localNow = TimeZoneInfo.ConvertTime(DateTime.Now, _timeZone);
                var utcNow = TimeZoneInfo.ConvertTimeToUtc(localNow, _timeZone);

                _logger.LogDebug("🕐 GetNowInUtc: {UtcNow}", utcNow);
                return utcNow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error obteniendo hora actual en UTC, usando DateTime.UtcNow");
                return DateTime.UtcNow;
            }
        }

        public DateTime GetLocalTimeInConfiguredTimeZone()
        {
            try
            {
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

                _logger.LogDebug("🕐 GetLocalTimeInConfiguredTimeZone: {LocalTime}", localTime);
                return localTime;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error obteniendo hora local, usando DateTime.Now");
                return DateTime.Now;
            }
        }
    }
}