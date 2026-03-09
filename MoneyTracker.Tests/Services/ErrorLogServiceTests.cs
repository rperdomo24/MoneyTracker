using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Infrastructure.Persistence;
using MoneyTracker.Infrastructure.Services;

namespace MoneyTracker.Tests.Services
{
    public class ErrorLogServiceTests
    {
        [Fact]
        public async Task LogAsync_WhenCalled_PersistsErrorLog()
        {
            await using var db = CreateDbContext();
            var service = new ErrorLogService(db, NullLogger<ErrorLogService>.Instance);

            await service.LogAsync(new ExceptionLogEntryDto
            {
                ExceptionType = "System.InvalidOperationException",
                Message = "Something failed",
                Path = "/auth/login",
                HttpMethod = "POST",
                TraceId = "trace-123",
                UserId = Guid.NewGuid(),
                TenantId = Guid.NewGuid()
            });

            var saved = await db.ErrorLogs.SingleAsync();
            saved.ExceptionType.Should().Be("System.InvalidOperationException");
            saved.Message.Should().Be("Something failed");
            saved.Path.Should().Be("/auth/login");
            saved.HttpMethod.Should().Be("POST");
            saved.TraceId.Should().Be("trace-123");
            saved.UserId.Should().NotBeNull();
            saved.TenantId.Should().NotBeNull();
        }

        [Fact]
        public async Task LogAsync_WhenMessageIsTooLong_TruncatesMessage()
        {
            await using var db = CreateDbContext();
            var service = new ErrorLogService(db, NullLogger<ErrorLogService>.Instance);
            var longMessage = new string('x', 4500);

            await service.LogAsync(new ExceptionLogEntryDto
            {
                ExceptionType = "System.Exception",
                Message = longMessage
            });

            var saved = await db.ErrorLogs.SingleAsync();
            saved.Message.Length.Should().Be(4000);
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
