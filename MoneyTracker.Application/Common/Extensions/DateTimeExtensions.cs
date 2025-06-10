namespace MoneyTracker.Application.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsThisMonth(this DateTime date)
        {
            var now = DateTime.Now;
            return date.Month == now.Month && date.Year == now.Year;
        }
    }
}
