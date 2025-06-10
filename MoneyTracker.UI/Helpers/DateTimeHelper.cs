namespace MoneyTracker.UI.Helpers;

public static class DateTimeHelpers
{
    #region Formatting Methods

    /// <summary>
    /// Obtiene una fecha formateada para mostrar (ej: "Dec 15, 2024")
    /// </summary>
    public static string GetFormattedDate(this DateTime date)
    {
        return date.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// Obtiene una fecha formateada para mostrar con null safety
    /// </summary>
    public static string GetFormattedDateSafe(this DateTime? date)
    {
        return date?.ToString("MMM dd, yyyy") ?? "No date";
    }

    /// <summary>
    /// Obtiene una fecha formateada corta (ej: "Dec 15")
    /// </summary>
    public static string GetShortFormattedDate(this DateTime date)
    {
        return date.ToString("MMM dd");
    }

    /// <summary>
    /// Obtiene una fecha formateada corta con null safety
    /// </summary>
    public static string GetShortFormattedDateSafe(this DateTime? date)
    {
        return date?.ToString("MMM dd") ?? "--";
    }

    /// <summary>
    /// Obtiene fecha y hora formateada (ej: "Dec 15, 2024 at 3:45 PM")
    /// </summary>
    public static string GetFormattedDateTime(this DateTime dateTime)
    {
        return dateTime.ToString("MMM dd, yyyy 'at' h:mm tt");
    }

    /// <summary>
    /// Obtiene fecha y hora formateada con null safety
    /// </summary>
    public static string GetFormattedDateTimeSafe(this DateTime? dateTime)
    {
        return dateTime?.ToString("MMM dd, yyyy 'at' h:mm tt") ?? "No date";
    }

    /// <summary>
    /// Obtiene solo la hora formateada (ej: "3:45 PM")
    /// </summary>
    public static string GetFormattedTime(this DateTime dateTime)
    {
        return dateTime.ToString("h:mm tt");
    }

    /// <summary>
    /// Obtiene fecha con día de la semana (ej: "December 15, 2024 • Monday")
    /// </summary>
    public static string GetFormattedDateWithDay(this DateTime date)
    {
        return $"{date:MMMM dd, yyyy} • {date:dddd}";
    }

    #endregion

    #region Relative Time Methods

    /// <summary>
    /// Determina si la fecha es de los últimos N días
    /// </summary>
    public static bool IsFromLastDays(this DateTime date, int days)
    {
        return (DateTime.Today - date.Date).TotalDays <= days;
    }

    /// <summary>
    /// Determina si la fecha es de hoy
    /// </summary>
    public static bool IsToday(this DateTime date)
    {
        return date.Date == DateTime.Today;
    }

    /// <summary>
    /// Determina si la fecha es de ayer
    /// </summary>
    public static bool IsYesterday(this DateTime date)
    {
        return date.Date == DateTime.Today.AddDays(-1);
    }

    /// <summary>
    /// Determina si la fecha es de esta semana
    /// </summary>
    public static bool IsThisWeek(this DateTime date)
    {
        var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        return date.Date >= startOfWeek && date.Date <= endOfWeek;
    }

    /// <summary>
    /// Determina si la fecha es de este mes
    /// </summary>
    public static bool IsThisMonth(this DateTime date)
    {
        return date.Month == DateTime.Now.Month && date.Year == DateTime.Now.Year;
    }

    /// <summary>
    /// Determina si la fecha es de este año
    /// </summary>
    public static bool IsThisYear(this DateTime date)
    {
        return date.Year == DateTime.Now.Year;
    }

    /// <summary>
    /// Obtiene el tiempo relativo (ej: "2 days ago", "Today", "Yesterday")
    /// </summary>
    public static string GetRelativeTime(this DateTime date)
    {
        var timeSpan = DateTime.Now - date;

        if (date.IsToday())
            return "Today";

        if (date.IsYesterday())
            return "Yesterday";

        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";

        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";

        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";

        return $"{(int)(timeSpan.TotalDays / 365)} years ago";
    }

    #endregion

    #region Date Range Methods

    /// <summary>
    /// Obtiene el primer día del mes
    /// </summary>
    public static DateTime GetFirstDayOfMonth(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }

    /// <summary>
    /// Obtiene el último día del mes
    /// </summary>
    public static DateTime GetLastDayOfMonth(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }

    /// <summary>
    /// Obtiene el primer día de la semana (lunes)
    /// </summary>
    public static DateTime GetFirstDayOfWeek(this DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    /// <summary>
    /// Obtiene el último día de la semana (domingo)
    /// </summary>
    public static DateTime GetLastDayOfWeek(this DateTime date)
    {
        return date.GetFirstDayOfWeek().AddDays(6);
    }

    /// <summary>
    /// Determina si una fecha está entre dos fechas (inclusive)
    /// </summary>
    public static bool IsBetween(this DateTime date, DateTime startDate, DateTime endDate)
    {
        return date.Date >= startDate.Date && date.Date <= endDate.Date;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Convierte a inicio del día (00:00:00)
    /// </summary>
    public static DateTime ToStartOfDay(this DateTime date)
    {
        return date.Date;
    }

    /// <summary>
    /// Convierte a final del día (23:59:59)
    /// </summary>
    public static DateTime ToEndOfDay(this DateTime date)
    {
        return date.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Obtiene solo la fecha sin hora
    /// </summary>
    public static DateTime DateOnly(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    #endregion
}