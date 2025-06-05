namespace MoneyTracker.Application.Interfaces
{
    public interface ITimeZoneService
    {
        DateTime ConvertToUtc(DateTime localDate);
        DateTime? ConvertToUtc(DateTime? localDate);
        DateTime ConvertFromUtc(DateTime utcDate);
        DateTime? ConvertFromUtc(DateTime? utcDate);
        DateTime GetNowInUtc();
        DateTime GetLocalTimeInConfiguredTimeZone();
    }
}
