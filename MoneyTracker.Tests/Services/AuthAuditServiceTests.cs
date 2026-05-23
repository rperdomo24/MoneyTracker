using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Infrastructure.Persistence;
using MoneyTracker.Infrastructure.Services;

namespace MoneyTracker.Tests.Services
{
    public class AuthAuditServiceTests
    {
        [Fact]
        public async Task LogAsync_WhenCalled_PersistsAuditLog()
        {
            await using var db = CreateDbContext();
            var service = new AuthAuditService(db, NullLogger<AuthAuditService>.Instance);

            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            await service.LogAsync(new AuthAuditEntryDto
            {
                Action = AuthAuditConstants.LoginPassword,
                Outcome = AuthAuditConstants.OutcomeFailure,
                FailureReason = "InvalidPassword",
                EmailMasked = "u***r@example.com",
                UserId = userId,
                TenantId = tenantId,
                IpAddress = "127.0.0.1",
                UserAgent = "UnitTestAgent",
                HttpMethod = "POST",
                Path = "/auth/login",
                TraceId = "trace-001"
            });

            var saved = await db.AuthAuditLogs.SingleAsync();
            saved.Action.Should().Be(AuthAuditConstants.LoginPassword);
            saved.Outcome.Should().Be(AuthAuditConstants.OutcomeFailure);
            saved.FailureReason.Should().Be("InvalidPassword");
            saved.EmailMasked.Should().Be("u***r@example.com");
            saved.UserId.Should().Be(userId);
            saved.TenantId.Should().Be(tenantId);
            saved.Path.Should().Be("/auth/login");
        }

        [Fact]
        public async Task LogAsync_WhenFieldsAreTooLong_TruncatesFields()
        {
            await using var db = CreateDbContext();
            var service = new AuthAuditService(db, NullLogger<AuthAuditService>.Instance);

            await service.LogAsync(new AuthAuditEntryDto
            {
                Action = new string('a', 100),
                Outcome = new string('o', 30),
                FailureReason = new string('r', 500),
                EmailMasked = new string('e', 200),
                IpAddress = new string('1', 80),
                UserAgent = new string('u', 2000),
                HttpMethod = new string('m', 30),
                Path = new string('p', 2000),
                TraceId = new string('t', 200)
            });

            var saved = await db.AuthAuditLogs.SingleAsync();
            saved.Action!.Length.Should().Be(64);
            saved.Outcome!.Length.Should().Be(16);
            saved.FailureReason!.Length.Should().Be(256);
            saved.EmailMasked!.Length.Should().Be(128);
            saved.IpAddress!.Length.Should().Be(64);
            saved.UserAgent!.Length.Should().Be(1024);
            saved.HttpMethod!.Length.Should().Be(16);
            saved.Path!.Length.Should().Be(1024);
            saved.TraceId!.Length.Should().Be(128);
        }

        private static MoneyTrackerDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MoneyTrackerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var tenantContext = new Mock<ITenantContext>();
            tenantContext.SetupGet(x => x.TenantId).Returns((Guid?)null);

            return new MoneyTrackerDbContext(options, tenantContext.Object);
        }
    }
}
