using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Application.DTOs.Gmail;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class EmailImportServiceTests
    {
        private readonly Mock<IGmailConnectionRepository> _connectionRepo = new();
        private readonly Mock<IEmailImportRuleRepository> _ruleRepo = new();
        private readonly Mock<IEmailImportItemRepository> _itemRepo = new();
        private readonly Mock<IGmailApiClient> _gmailClient = new();
        private readonly Mock<IEmailSyncEngine> _syncEngine = new();
        private readonly Mock<IEmailSyncScheduler> _scheduler = new();
        private readonly Mock<ITenantContext> _tenantContext = new();
        private readonly Mock<ITokenProtector> _protector = new();
        private readonly EmailImportService _service;

        private static readonly Guid TenantId = Guid.NewGuid();

        public EmailImportServiceTests()
        {
            _tenantContext.Setup(t => t.TenantId).Returns(TenantId);

            _service = new EmailImportService(
                _connectionRepo.Object,
                _ruleRepo.Object,
                _itemRepo.Object,
                _gmailClient.Object,
                _syncEngine.Object,
                _scheduler.Object,
                _tenantContext.Object,
                _protector.Object,
                NullLogger<EmailImportService>.Instance);
        }

        [Fact]
        public async Task GetConnectionAsync_WhenConnected_ReturnsDto()
        {
            _connectionRepo.Setup(r => r.GetActiveAsync())
                .ReturnsAsync(new GmailConnection { Id = 1, Email = "user@gmail.com", TenantId = TenantId });

            var result = await _service.GetConnectionAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Email.Should().Be("user@gmail.com");
        }

        [Fact]
        public async Task GetConnectionAsync_WhenRepositoryThrows_ReturnsFail()
        {
            _connectionRepo.Setup(r => r.GetActiveAsync()).ThrowsAsync(new Exception("boom"));

            var result = await _service.GetConnectionAsync();

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task DisconnectAsync_WhenNoConnection_ReturnsFail()
        {
            _connectionRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync((GmailConnection?)null);

            var result = await _service.DisconnectAsync();

            result.Success.Should().BeFalse();
            _connectionRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DisconnectAsync_WhenConnected_DeletesAndDisablesAutoSync()
        {
            _connectionRepo.Setup(r => r.GetActiveAsync())
                .ReturnsAsync(new GmailConnection { Id = 7, TenantId = TenantId });

            var result = await _service.DisconnectAsync();

            result.Success.Should().BeTrue();
            _connectionRepo.Verify(r => r.DeleteAsync(7), Times.Once);
            _scheduler.Verify(s => s.DisableAutoSync(TenantId), Times.Once);
        }

        [Fact]
        public async Task SetAutoSyncAsync_WhenIntervalTooLow_ReturnsFail()
        {
            var result = await _service.SetAutoSyncAsync(true, 5);

            result.Success.Should().BeFalse();
            _connectionRepo.Verify(r => r.GetActiveAsync(), Times.Never);
        }

        [Fact]
        public async Task SetAutoSyncAsync_WhenEnabledWithValidInterval_EnablesScheduler()
        {
            _connectionRepo.Setup(r => r.GetActiveAsync())
                .ReturnsAsync(new GmailConnection { Id = 1, TenantId = TenantId });

            var result = await _service.SetAutoSyncAsync(true, 30);

            result.Success.Should().BeTrue();
            _scheduler.Verify(s => s.EnableAutoSync(TenantId, 30), Times.Once);
        }

        [Fact]
        public async Task SyncNowAsync_WhenNoConnection_ReturnsFail()
        {
            _connectionRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync((GmailConnection?)null);

            var result = await _service.SyncNowAsync();

            result.Success.Should().BeFalse();
            _syncEngine.Verify(e => e.SyncTenantAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SyncNowAsync_WhenConnected_RunsEngineAndUpdatesLastSync()
        {
            _connectionRepo.Setup(r => r.GetActiveAsync())
                .ReturnsAsync(new GmailConnection { Id = 1, TenantId = TenantId });
            _syncEngine.Setup(e => e.SyncTenantAsync(TenantId)).ReturnsAsync(3);

            var result = await _service.SyncNowAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().Be(3);
            _connectionRepo.Verify(r => r.UpdateAsync(It.Is<GmailConnection>(c => c.LastSyncAtUtc != null)), Times.Once);
        }

        [Fact]
        public async Task MarkImportedAsync_WhenItemNotFound_ReturnsFail()
        {
            _itemRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((EmailImportItem?)null);

            var result = await _service.MarkImportedAsync(99);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task MarkImportedAsync_WhenItemExists_SetsImportedStatus()
        {
            var item = new EmailImportItem { Id = 5, Status = EmailImportStatus.Pending, TenantId = TenantId };
            _itemRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(item);

            var result = await _service.MarkImportedAsync(5);

            result.Success.Should().BeTrue();
            item.Status.Should().Be(EmailImportStatus.Imported);
            _itemRepo.Verify(r => r.UpdateAsync(item), Times.Once);
        }

        [Fact]
        public async Task DismissAsync_WhenItemExists_SetsDismissedStatus()
        {
            var item = new EmailImportItem { Id = 8, Status = EmailImportStatus.Pending, TenantId = TenantId };
            _itemRepo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(item);

            var result = await _service.DismissAsync(8);

            result.Success.Should().BeTrue();
            item.Status.Should().Be(EmailImportStatus.Dismissed);
        }

        [Fact]
        public async Task DismissAsync_WhenItemMissing_ReturnsFail()
        {
            _itemRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((EmailImportItem?)null);

            var result = await _service.DismissAsync(123);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task SaveRuleAsync_WhenNewRule_CallsAdd()
        {
            var dto = new EmailImportRuleDto { SenderPattern = "bank.com", BankLabel = "Bank" };

            var result = await _service.SaveRuleAsync(dto);

            result.Success.Should().BeTrue();
            _ruleRepo.Verify(r => r.AddAsync(It.IsAny<EmailImportRule>()), Times.Once);
        }

        [Fact]
        public async Task SaveRuleAsync_WhenExistingRuleMissing_ReturnsFail()
        {
            _ruleRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((EmailImportRule?)null);
            var dto = new EmailImportRuleDto { Id = 42, SenderPattern = "bank.com", BankLabel = "Bank" };

            var result = await _service.SaveRuleAsync(dto);

            result.Success.Should().BeFalse();
            _ruleRepo.Verify(r => r.UpdateAsync(It.IsAny<EmailImportRule>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRuleAsync_WhenRuleMissing_ReturnsFail()
        {
            _ruleRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((EmailImportRule?)null);

            var result = await _service.DeleteRuleAsync(1);

            result.Success.Should().BeFalse();
        }
    }
}
