using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.DTOs.Gmail;
using MoneyTracker.Application.DTOs.TextImport;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Enums.Ai;
using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Integrations.Gmail;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class EmailSyncEngineTests
    {
        private readonly Mock<IGmailConnectionRepository> _connectionRepo = new();
        private readonly Mock<IEmailImportRuleRepository> _ruleRepo = new();
        private readonly Mock<IEmailImportItemRepository> _itemRepo = new();
        private readonly Mock<IGmailApiClient> _client = new();
        private readonly Mock<ITextImportService> _textImportService = new();
        private readonly Mock<ITokenProtector> _protector = new();
        private readonly Mock<ITimeZoneService> _timeZoneService = new();
        private readonly EmailSyncEngine _engine;

        private static readonly Guid TenantId = Guid.NewGuid();

        public EmailSyncEngineTests()
        {
            _protector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns("refresh-token");
            _timeZoneService.Setup(t => t.GetNowInUtc()).Returns(DateTime.UtcNow);

            _engine = new EmailSyncEngine(
                _connectionRepo.Object,
                _ruleRepo.Object,
                _itemRepo.Object,
                _client.Object,
                _textImportService.Object,
                _protector.Object,
                _timeZoneService.Object,
                Options.Create(new GmailSettings { MaxMessagesPerSync = 25 }),
                NullLogger<EmailSyncEngine>.Instance);

            _connectionRepo.Setup(r => r.GetActiveByTenantAsync(TenantId))
                .ReturnsAsync(new GmailConnection { TenantId = TenantId, EncryptedRefreshToken = "enc" });

            _ruleRepo.Setup(r => r.GetActiveByTenantAsync(TenantId))
                .ReturnsAsync(new List<EmailImportRule>
                {
                    new() { TenantId = TenantId, SenderPattern = "bancocuscatlan.com", BankLabel = "Cuscatlan", SubjectExcludeKeywords = "inicio de sesion, codigo" }
                });
        }

        [Fact]
        public async Task SyncTenantAsync_WhenSenderMatchesSecondPatternInCommaList_ProcessesMessage()
        {
            _ruleRepo.Setup(r => r.GetActiveByTenantAsync(TenantId))
                .ReturnsAsync(new List<EmailImportRule>
                {
                    new() { TenantId = TenantId, SenderPattern = "bancocuscatlan.com, alertas@otrobanco.com", BankLabel = "Multi" }
                });

            _client.Setup(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "msg-6" });
            _itemRepo.Setup(r => r.ExistsByMessageIdAsync(TenantId, "msg-6")).ReturnsAsync(false);
            _client.Setup(c => c.GetMessageAsync("refresh-token", "msg-6"))
                .ReturnsAsync(new GmailMessageDto
                {
                    Id = "msg-6",
                    From = "alertas@otrobanco.com",
                    Subject = "Compra realizada",
                    ReceivedAtUtc = DateTime.UtcNow,
                    BodyText = "Compra por $15.00 en Tienda W"
                });

            var analysis = new TextImportAnalysisDto();
            analysis.Items.Add(new ParsedTransactionSuggestionDto
            {
                Type = TransactionTypeEnum.Expense,
                Amount = 15.00m,
                Currency = "USD",
                DateLocal = new DateTime(2026, 1, 10),
                AccountHint = "9999"
            });
            _textImportService.Setup(s => s.AnalyzeAsync(It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TextImportAnalysisDto>.Ok(analysis));
            _itemRepo.Setup(r => r.ExistsByFingerprintAsync(TenantId, It.IsAny<string>())).ReturnsAsync(false);

            var found = await _engine.SyncTenantAsync(TenantId);

            found.Should().Be(1);
            _itemRepo.Verify(r => r.AddAsync(It.Is<EmailImportItem>(i => i.Status == EmailImportStatus.Pending)), Times.Once);
        }

        [Fact]
        public async Task SyncTenantAsync_BuildsGmailQueryWithAllPatternsFromCommaList()
        {
            _ruleRepo.Setup(r => r.GetActiveByTenantAsync(TenantId))
                .ReturnsAsync(new List<EmailImportRule>
                {
                    new() { TenantId = TenantId, SenderPattern = "bancocuscatlan.com, otrobanco.com" }
                });

            string? capturedQuery = null;
            _client.Setup(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Callback<string, string, int>((_, query, _) => capturedQuery = query)
                .ReturnsAsync(new List<string>());

            await _engine.SyncTenantAsync(TenantId);

            capturedQuery.Should().Contain("from:bancocuscatlan.com");
            capturedQuery.Should().Contain("from:otrobanco.com");
        }

        [Fact]
        public async Task SyncTenantAsync_WhenNoConnection_ReturnsZero()
        {
            _connectionRepo.Setup(r => r.GetActiveByTenantAsync(TenantId)).ReturnsAsync((GmailConnection?)null);

            var found = await _engine.SyncTenantAsync(TenantId);

            found.Should().Be(0);
            _client.Verify(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SyncTenantAsync_WhenLoginNotificationSubject_IsIgnoredWithoutCallingAi()
        {
            _client.Setup(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "msg-1" });
            _itemRepo.Setup(r => r.ExistsByMessageIdAsync(TenantId, "msg-1")).ReturnsAsync(false);
            _client.Setup(c => c.GetMessageAsync("refresh-token", "msg-1"))
                .ReturnsAsync(new GmailMessageDto
                {
                    Id = "msg-1",
                    From = "notificaciones@bancocuscatlan.com",
                    Subject = "Codigo de inicio de sesion",
                    ReceivedAtUtc = DateTime.UtcNow,
                    BodyText = "Your login code is 123456"
                });

            var found = await _engine.SyncTenantAsync(TenantId);

            found.Should().Be(0);
            _textImportService.Verify(s => s.AnalyzeAsync(It.IsAny<string>()), Times.Never);
            _itemRepo.Verify(r => r.AddAsync(It.Is<EmailImportItem>(i => i.Status == EmailImportStatus.Ignored)), Times.Once);
        }

        [Fact]
        public async Task SyncTenantAsync_WhenAiFindsNoTransaction_MarksNotTransaction()
        {
            _client.Setup(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "msg-2" });
            _itemRepo.Setup(r => r.ExistsByMessageIdAsync(TenantId, "msg-2")).ReturnsAsync(false);
            _client.Setup(c => c.GetMessageAsync("refresh-token", "msg-2"))
                .ReturnsAsync(new GmailMessageDto
                {
                    Id = "msg-2",
                    From = "notificaciones@bancocuscatlan.com",
                    Subject = "Promotional offer",
                    ReceivedAtUtc = DateTime.UtcNow,
                    BodyText = "50% off your next purchase"
                });
            _textImportService.Setup(s => s.AnalyzeAsync(It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TextImportAnalysisDto>.Fail("No recognizable transaction was found."));

            var found = await _engine.SyncTenantAsync(TenantId);

            found.Should().Be(0);
            _itemRepo.Verify(r => r.AddAsync(It.Is<EmailImportItem>(i => i.Status == EmailImportStatus.NotTransaction)), Times.Once);
        }

        [Fact]
        public async Task SyncTenantAsync_WhenAiCallFailsTransiently_DoesNotPersistItemSoItRetries()
        {
            _client.Setup(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "msg-5" });
            _itemRepo.Setup(r => r.ExistsByMessageIdAsync(TenantId, "msg-5")).ReturnsAsync(false);
            _client.Setup(c => c.GetMessageAsync("refresh-token", "msg-5"))
                .ReturnsAsync(new GmailMessageDto
                {
                    Id = "msg-5",
                    From = "notificaciones@bancocuscatlan.com",
                    Subject = "Compra realizada",
                    ReceivedAtUtc = DateTime.UtcNow,
                    BodyText = "Compra por $10.00 en Tienda Z"
                });
            _textImportService.Setup(s => s.AnalyzeAsync(It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TextImportAnalysisDto>.Fail("AI analysis failed. Check logs for details."));

            var found = await _engine.SyncTenantAsync(TenantId);

            found.Should().Be(0);
            _itemRepo.Verify(r => r.AddAsync(It.IsAny<EmailImportItem>()), Times.Never);
        }

        [Fact]
        public async Task SyncTenantAsync_WhenFingerprintAlreadyExists_MarksDuplicate()
        {
            _client.Setup(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "msg-3" });
            _itemRepo.Setup(r => r.ExistsByMessageIdAsync(TenantId, "msg-3")).ReturnsAsync(false);
            _client.Setup(c => c.GetMessageAsync("refresh-token", "msg-3"))
                .ReturnsAsync(new GmailMessageDto
                {
                    Id = "msg-3",
                    From = "notificaciones@bancocuscatlan.com",
                    Subject = "Compra realizada",
                    ReceivedAtUtc = DateTime.UtcNow,
                    BodyText = "Compra por $25.00 en Tienda X"
                });

            var analysis = new TextImportAnalysisDto();
            analysis.Items.Add(new ParsedTransactionSuggestionDto
            {
                Type = TransactionTypeEnum.Expense,
                Amount = 25.00m,
                Currency = "USD",
                DateLocal = new DateTime(2026, 1, 10),
                AccountHint = "1234"
            });
            _textImportService.Setup(s => s.AnalyzeAsync(It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TextImportAnalysisDto>.Ok(analysis));

            _itemRepo.Setup(r => r.ExistsByFingerprintAsync(TenantId, It.IsAny<string>())).ReturnsAsync(true);

            var found = await _engine.SyncTenantAsync(TenantId);

            found.Should().Be(0);
            _itemRepo.Verify(r => r.AddAsync(It.Is<EmailImportItem>(i => i.Status == EmailImportStatus.Duplicate)), Times.Once);
        }

        [Fact]
        public async Task SyncTenantAsync_WhenValidNewTransaction_MarksPendingAndReturnsOne()
        {
            _client.Setup(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "msg-4" });
            _itemRepo.Setup(r => r.ExistsByMessageIdAsync(TenantId, "msg-4")).ReturnsAsync(false);
            _client.Setup(c => c.GetMessageAsync("refresh-token", "msg-4"))
                .ReturnsAsync(new GmailMessageDto
                {
                    Id = "msg-4",
                    From = "notificaciones@bancocuscatlan.com",
                    Subject = "Compra realizada",
                    ReceivedAtUtc = DateTime.UtcNow,
                    BodyText = "Compra por $40.00 en Tienda Y"
                });

            var analysis = new TextImportAnalysisDto();
            analysis.Items.Add(new ParsedTransactionSuggestionDto
            {
                Type = TransactionTypeEnum.Expense,
                Amount = 40.00m,
                Currency = "USD",
                DateLocal = new DateTime(2026, 1, 10),
                AccountHint = "5678"
            });
            _textImportService.Setup(s => s.AnalyzeAsync(It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TextImportAnalysisDto>.Ok(analysis));

            _itemRepo.Setup(r => r.ExistsByFingerprintAsync(TenantId, It.IsAny<string>())).ReturnsAsync(false);

            var found = await _engine.SyncTenantAsync(TenantId);

            found.Should().Be(1);
            _itemRepo.Verify(r => r.AddAsync(It.Is<EmailImportItem>(i => i.Status == EmailImportStatus.Pending)), Times.Once);
        }

        [Fact]
        public async Task SyncTenantAsync_WhenMessageCountHitsMax_DoesNotAdvanceLastSyncAtUtc()
        {
            var maxIds = Enumerable.Range(0, 25).Select(i => $"msg-{i}").ToList();
            _client.Setup(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(maxIds);
            _itemRepo.Setup(r => r.ExistsByMessageIdAsync(TenantId, It.IsAny<string>())).ReturnsAsync(true);

            await _engine.SyncTenantAsync(TenantId);

            _connectionRepo.Verify(r => r.UpdateLastSyncByTenantAsync(TenantId, It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task SyncTenantAsync_WhenMessageCountUnderMax_AdvancesLastSyncAtUtc()
        {
            _client.Setup(c => c.ListMessageIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "msg-only-one" });
            _itemRepo.Setup(r => r.ExistsByMessageIdAsync(TenantId, It.IsAny<string>())).ReturnsAsync(true);

            await _engine.SyncTenantAsync(TenantId);

            _connectionRepo.Verify(r => r.UpdateLastSyncByTenantAsync(TenantId, It.IsAny<DateTime>()), Times.Once);
        }
    }
}
