using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Infrastructure.Persistence;
using MoneyTracker.Infrastructure.Services;

namespace MoneyTracker.Tests.Services
{
    public class VerificationCodeServiceTests
    {
        [Fact]
        public async Task IssueCodeAsync_WhenNoActiveCode_ReturnsNewCode()
        {
            await using var db = CreateDbContext();
            var service = new VerificationCodeService(db, CreateErrorLogService().Object);
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            var result = await service.IssueCodeAsync(userId, tenantId, "login_otp", 10, 60, 3);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Code.Should().HaveLength(6);
            db.UserVerificationCodes.Should().HaveCount(1);
        }

        [Fact]
        public async Task IssueCodeAsync_WhenCooldownActive_ReturnsFail()
        {
            await using var db = CreateDbContext();
            var service = new VerificationCodeService(db, CreateErrorLogService().Object);
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            var first = await service.IssueCodeAsync(userId, tenantId, "login_otp", 10, 120, 3);
            first.Success.Should().BeTrue();

            var second = await service.IssueCodeAsync(userId, tenantId, "login_otp", 10, 120, 3);
            second.Success.Should().BeFalse();
            second.Message.Should().Contain("Please wait");
        }

        [Fact]
        public async Task VerifyCodeAsync_WhenCodeMatches_MarksAsConsumed()
        {
            await using var db = CreateDbContext();
            var service = new VerificationCodeService(db, CreateErrorLogService().Object);
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            var issue = await service.IssueCodeAsync(userId, tenantId, "login_otp", 10, 1, 3);
            issue.Success.Should().BeTrue();

            var verify = await service.VerifyCodeAsync(userId, tenantId, "login_otp", issue.Data!.Code);
            verify.Success.Should().BeTrue();

            var saved = await db.UserVerificationCodes.SingleAsync();
            saved.ConsumedAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task VerifyCodeAsync_WhenExpired_InvalidatesRecordAndFails()
        {
            await using var db = CreateDbContext();
            var service = new VerificationCodeService(db, CreateErrorLogService().Object);
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            var issue = await service.IssueCodeAsync(userId, tenantId, "login_otp", 10, 1, 3);
            issue.Success.Should().BeTrue();

            var record = await db.UserVerificationCodes.SingleAsync();
            record.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();

            var verify = await service.VerifyCodeAsync(userId, tenantId, "login_otp", issue.Data!.Code);
            verify.Success.Should().BeFalse();
            verify.Message.Should().Be("Verification code expired.");

            var updated = await db.UserVerificationCodes.SingleAsync();
            updated.InvalidatedAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task VerifyCodeAsync_WhenMaxAttemptsReached_InvalidatesRecord()
        {
            await using var db = CreateDbContext();
            var service = new VerificationCodeService(db, CreateErrorLogService().Object);
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            var issue = await service.IssueCodeAsync(userId, tenantId, "login_otp", 10, 1, 2);
            issue.Success.Should().BeTrue();

            var firstFail = await service.VerifyCodeAsync(userId, tenantId, "login_otp", "111111");
            firstFail.Success.Should().BeFalse();

            var secondFail = await service.VerifyCodeAsync(userId, tenantId, "login_otp", "222222");
            secondFail.Success.Should().BeFalse();

            var record = await db.UserVerificationCodes.SingleAsync();
            record.AttemptCount.Should().Be(2);
            record.InvalidatedAtUtc.Should().NotBeNull();
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

        private static Mock<IErrorLogService> CreateErrorLogService()
        {
            var errorLogService = new Mock<IErrorLogService>();
            errorLogService.Setup(x => x.LogAsync(It.IsAny<MoneyTracker.Application.DTOs.ExceptionLogEntryDto>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            errorLogService.Setup(x => x.LogExceptionAsync(It.IsAny<Exception>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            errorLogService.Setup(x => x.LogMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return errorLogService;
        }
    }
}
