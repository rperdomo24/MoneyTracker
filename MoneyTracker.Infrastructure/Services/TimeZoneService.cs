
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.Interfaces;
using System.Runtime;

namespace MoneyTracker.Infrastructure.Services
{
    public class TimeZoneService : ITimeZoneService
    {
        private readonly TimeZoneInfo _timeZone;

        public TimeZoneService(IOptions<ApplicationSettings> options)
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.DefaultTimeZone);
        }

        public DateTime ConvertToUtc(DateTime localDate)
        {
            try
            {
                // Asegurar que el DateTime es 'Unspecified' para indicar que pertenece a esa zona
                var dateWithCorrectKind = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);

                return TimeZoneInfo.ConvertTimeToUtc(dateWithCorrectKind, _timeZone);
            }
            catch (Exception)
            {
                Console.WriteLine($"[ERROR] No se encontró el TimeZone '{_timeZone}', usando UTC.");
                return localDate.ToUniversalTime(); // fallback
            }
        }

        public DateTime? ConvertToUtc(DateTime? localDate)
        {
            if (localDate == null)
                return null;


            try
            {
                return ConvertToUtc(localDate.Value);
            }
            catch (Exception)
            {
                Console.WriteLine($"[ERROR] No se encontró el TimeZone '{_timeZone}', usando UTC.");
                return localDate.Value.ToUniversalTime();
            }
        }

        public DateTime ConvertFromUtc(DateTime utcDate)
        {

            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utcDate, _timeZone);
            }
            catch
            {
                Console.WriteLine($"[ERROR] No se encontró el TimeZone '{_timeZone}', retornando UTC.");
                return utcDate; // Fallback: devolver UTC sin cambios
            }
        }

        public DateTime? ConvertFromUtc(DateTime? utcDate)
        {
            if (!utcDate.HasValue)
                return null; // Si la fecha es null, devolver null

            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utcDate.Value, _timeZone);
            }
            catch
            {
                Console.WriteLine($"[ERROR] No se encontró el TimeZone '{_timeZone}', retornando null.");
                return null; // Si hay error, devolver null
            }
        }

        public DateTime GetNowInUtc()
        {

            try
            {
                var localNow = TimeZoneInfo.ConvertTime(DateTime.Now, _timeZone); // 'Ahora' en zona horaria configurada
                return TimeZoneInfo.ConvertTimeToUtc(localNow, _timeZone);
            }
            catch
            {
                Console.WriteLine($"[ERROR] No se encontró el TimeZone '{_timeZone}', usando UTC.");
                return DateTime.UtcNow; // Fallback a UTC si hay error
            }
        }

        public DateTime GetLocalTimeInConfiguredTimeZone()
        {

            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
            }
            catch
            {
                Console.WriteLine($"[ERROR] No se encontró el TimeZone '{_timeZone}', retornando DateTime.Now local.");
                return DateTime.Now; // Fallback: hora local del servidor
            }
        }
    }
}
