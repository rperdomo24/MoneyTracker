namespace MoneyTracker.Application.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsThisMonth(this DateTime date)
        {
            var now = DateTime.Now;
            return date.Month == now.Month && date.Year == now.Year;
        }

        public static bool IsLastMonth(this DateTime date)
        {
            var lastMonth = DateTime.Now.AddMonths(-1);
            return date.Year == lastMonth.Year && date.Month == lastMonth.Month;
        }

        public static bool IsThisYear(this DateTime date)
        {
            return date.Year == DateTime.Now.Year;
        }

        public static bool IsBetween(this DateTime date, DateTime startDate, DateTime endDate)
        {
            return date.Date >= startDate.Date && date.Date <= endDate.Date;
        }
    }
}
