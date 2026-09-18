namespace MoneyTracker.Application.Common
{
    public static class HalfMonthRangeHelper
    {
        public static (DateTime LocalStart, DateTime LocalEnd) GetLocalRange(int year, int month, int halfMonth)
        {
            if (halfMonth == 1)
            {
                return (
                    new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    new DateTime(year, month, 15, 23, 59, 59, DateTimeKind.Unspecified));
            }

            if (halfMonth == 2)
            {
                return (
                    new DateTime(year, month, 16, 0, 0, 0, DateTimeKind.Unspecified),
                    new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Unspecified));
            }

            var localStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return (localStart, localStart.AddMonths(1).AddTicks(-1));
        }
    }
}
