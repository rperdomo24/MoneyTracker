using System.Net;
using System.Text;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;

namespace MoneyTracker.Infrastructure.Jobs
{
    /// <summary>
    /// Builds a single daily digest email grouping every notification created
    /// by <see cref="NotificationGeneratorJob"/> in one run, instead of one email per alert.
    /// </summary>
    public static class NotificationDigestEmailBuilder
    {
        private static readonly (NotificationType Type, string Section, string Accent, int Order)[] SectionMap =
        [
            (NotificationType.BudgetAlert, "Budgets", "#E53935", 1),
            (NotificationType.LoanReminder, "Loans", "#1976D2", 2),
            (NotificationType.CreditCardCut, "Credit Cards", "#1976D2", 3),
            (NotificationType.CreditCardDue, "Credit Cards", "#1976D2", 3),
            (NotificationType.CalendarReminder, "Reminders", "#6366f1", 4),
            (NotificationType.GoalContributionReminder, "Savings Goals", "#10b981", 5),
            (NotificationType.General, "General", "#2143B5", 6),
        ];

        public static string BuildSubject(int count) =>
            count == 1 ? "MoneyTracker daily summary — 1 alert" : $"MoneyTracker daily summary — {count} alerts";

        public static string Build(IReadOnlyList<AppNotification> notifications, string? publicBaseUrl)
        {
            var baseUrl = publicBaseUrl?.TrimEnd('/');

            var sections = notifications
                .GroupBy(n => SectionFor(n.Type))
                .OrderBy(g => g.Key.Order)
                .ToList();

            var sb = new StringBuilder();
            sb.Append($$"""
                <!DOCTYPE html>
                <html>
                <body style="font-family:'Segoe UI',sans-serif;background:#f4f7f6;margin:0;padding:32px;">
                  <div style="max-width:560px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);">
                    <div style="background:#2143B5;padding:24px;text-align:center;">
                      <h2 style="color:#fff;margin:0;">MoneyTracker — Daily Summary</h2>
                      <p style="color:rgba(255,255,255,0.85);margin:6px 0 0;">{{notifications.Count}} alert(s) today</p>
                    </div>
                    <div style="padding:8px 32px 24px;">
                """);

            foreach (var section in sections)
            {
                var accent = section.Key.Accent;
                sb.Append($"""
                      <h3 style="color:{accent};font-size:15px;margin:20px 0 8px;text-transform:uppercase;letter-spacing:0.5px;">{Encode(section.Key.Section)}</h3>
                """);

                foreach (var n in section)
                {
                    var link = BuildLink(baseUrl, n.Link);
                    sb.Append($"""
                          <div style="background:#f8f9fa;border-left:4px solid {accent};border-radius:4px;padding:12px 16px;margin-bottom:10px;">
                            <p style="margin:0;font-weight:600;color:#2A364E;font-size:14px;">{Encode(n.Title)}</p>
                            <p style="margin:4px 0 0;color:#555;font-size:13px;">{Encode(n.Message)}</p>
                            {(link is null ? "" : $"<a href=\"{link}\" style=\"display:inline-block;margin-top:8px;font-size:12px;font-weight:600;color:{accent};text-decoration:none;\">View &rarr;</a>")}
                          </div>
                    """);
                }
            }

            sb.Append("""
                    </div>
                    <div style="background:#f4f7f6;padding:16px;text-align:center;">
                      <p style="margin:0;font-size:12px;color:#999;">MoneyTracker &mdash; Daily notification summary</p>
                    </div>
                  </div>
                </body>
                </html>
                """);

            return sb.ToString();
        }

        private static (string Section, string Accent, int Order) SectionFor(NotificationType type)
        {
            foreach (var entry in SectionMap)
            {
                if (entry.Type == type)
                    return (entry.Section, entry.Accent, entry.Order);
            }

            return ("General", "#2143B5", 99);
        }

        private static string? BuildLink(string? baseUrl, string? link)
        {
            if (string.IsNullOrWhiteSpace(link)) return null;
            if (string.IsNullOrWhiteSpace(baseUrl)) return null;
            return $"{baseUrl}{link}";
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
