using FluentAssertions;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Infrastructure.Jobs;

namespace MoneyTracker.Tests.Services
{
    public class NotificationDigestEmailBuilderTests
    {
        [Fact]
        public void Build_GroupsNotificationsByTypeSection()
        {
            var notifications = new List<AppNotification>
            {
                new() { Title = "Budget exceeded: Food", Message = "You spent too much", Type = NotificationType.BudgetAlert, Link = "/budgets" },
                new() { Title = "Loan payment due today", Message = "Pay your loan", Type = NotificationType.LoanReminder, Link = "/loans" },
                new() { Title = "Statement closes today", Message = "Card closing", Type = NotificationType.CreditCardCut, Link = "/accounts/1" }
            };

            var html = NotificationDigestEmailBuilder.Build(notifications, "https://app.example.com");

            html.Should().Contain("Budgets");
            html.Should().Contain("Loans");
            html.Should().Contain("Credit Cards");
            html.Should().Contain("Budget exceeded: Food");
            html.Should().Contain("Loan payment due today");
        }

        [Fact]
        public void Build_WithBaseUrl_ProducesAbsoluteLinks()
        {
            var notifications = new List<AppNotification>
            {
                new() { Title = "Budget exceeded: Food", Message = "You spent too much", Type = NotificationType.BudgetAlert, Link = "/budgets" }
            };

            var html = NotificationDigestEmailBuilder.Build(notifications, "https://app.example.com");

            html.Should().Contain("href=\"https://app.example.com/budgets\"");
        }

        [Fact]
        public void Build_WithoutBaseUrl_OmitsLinkButton()
        {
            var notifications = new List<AppNotification>
            {
                new() { Title = "Budget exceeded: Food", Message = "You spent too much", Type = NotificationType.BudgetAlert, Link = "/budgets" }
            };

            var html = NotificationDigestEmailBuilder.Build(notifications, string.Empty);

            html.Should().NotContain("href=");
        }

        [Fact]
        public void Build_EscapesHtmlInTitleAndMessage()
        {
            var notifications = new List<AppNotification>
            {
                new() { Title = "<script>alert(1)</script>", Message = "Balance < 0 & rising", Type = NotificationType.General }
            };

            var html = NotificationDigestEmailBuilder.Build(notifications, null);

            html.Should().NotContain("<script>alert(1)</script>");
            html.Should().Contain("&lt;script&gt;");
            html.Should().Contain("&amp;");
        }

        [Fact]
        public void BuildSubject_UsesSingularForOneAlert()
        {
            NotificationDigestEmailBuilder.BuildSubject(1).Should().Be("MoneyTracker daily summary — 1 alert");
            NotificationDigestEmailBuilder.BuildSubject(3).Should().Be("MoneyTracker daily summary — 3 alerts");
        }
    }
}
