namespace MoneyTracker.UI.Helpers
{
    public static class DateTimeHelper
    {
        public static string ToRelativeTime(DateTime dateTime)
        {
            var ts = DateTime.UtcNow - dateTime.ToUniversalTime();

            if (ts.TotalSeconds < 60)
                return "just now";
            if (ts.TotalMinutes < 60)
                return $"{(int)ts.TotalMinutes}m ago";
            if (ts.TotalHours < 24)
                return $"{(int)ts.TotalHours}h ago";
            if (ts.TotalDays < 7)
                return $"{(int)ts.TotalDays}d ago";
            if (ts.TotalDays < 30)
                return $"{(int)(ts.TotalDays / 7)}w ago";
            if (ts.TotalDays < 365)
                return $"{(int)(ts.TotalDays / 30)}mo ago";

            return $"{(int)(ts.TotalDays / 365)}y ago";
        }
    }
}
