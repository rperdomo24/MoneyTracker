using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Gmail;
using MoneyTracker.Application.DTOs.TextImport;
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
        private readonly Mock<ITextImportService> _textImportService = new();
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
                _textImportService.Object,
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
        public async Task SyncNowAsync_WhenConnected_RunsEngine()
        {
            _connectionRepo.Setup(r => r.GetActiveAsync())
                .ReturnsAsync(new GmailConnection { Id = 1, TenantId = TenantId });
            _syncEngine.Setup(e => e.SyncTenantAsync(TenantId)).ReturnsAsync(3);

            var result = await _service.SyncNowAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().Be(3);
            _syncEngine.Verify(e => e.SyncTenantAsync(TenantId), Times.Once);
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
        public async Task SaveRuleAsync_WhenExistingRule_CallsUpdate()
        {
            var entity = new EmailImportRule
            {
                Id = 42,
                SenderPattern = "old.com",
                BankLabel = "Old Bank",
                SubjectExcludeKeywords = "old"
            };
            _ruleRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(entity);
            var dto = new EmailImportRuleDto
            {
                Id = 42,
                SenderPattern = "newbank.com",
                BankLabel = "New Bank",
                SubjectExcludeKeywords = "new"
            };

            var result = await _service.SaveRuleAsync(dto);

            result.Success.Should().BeTrue();
            _ruleRepo.Verify(r => r.UpdateAsync(entity), Times.Once);
            _ruleRepo.Verify(r => r.AddAsync(It.IsAny<EmailImportRule>()), Times.Never);
            entity.SenderPattern.Should().Be("newbank.com");
            entity.BankLabel.Should().Be("New Bank");
            entity.SubjectExcludeKeywords.Should().Be("new");
        }

        [Fact]
        public async Task DeleteRuleAsync_WhenRuleMissing_ReturnsFail()
        {
            _ruleRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((EmailImportRule?)null);

            var result = await _service.DeleteRuleAsync(1);

            result.Success.Should().BeFalse();
        }

        // Mirrors what EmailSyncEngine actually persists: JsonSerializer.Serialize of
        // ParsedTransactionSuggestionDto with no enum converter, so Type is numeric (Expense = 1).
        private const string ValidParsedJson = """
            {"items":[{"type":1,"amount":10.50,"currency":"USD","dateLocal":"2026-05-23T12:13:00","merchant":"Store","description":"desc","provider":"BANK","accountHint":"1234","confidence":0.9,"warnings":[]}]}
            """;

        [Fact]
        public async Task GetReviewSuggestionsAsync_WhenNoIdsGiven_ReturnsFail()
        {
            var result = await _service.GetReviewSuggestionsAsync(Array.Empty<int>());

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetReviewSuggestionsAsync_WhenItemHasValidParsedJson_UsesStoredJsonWithoutCallingAi()
        {
            var item = new EmailImportItem
            {
                Id = 1,
                TenantId = TenantId,
                ParsedJson = ValidParsedJson,
                AiTrainingDataId = 55,
                Status = EmailImportStatus.Pending
            };
            _itemRepo.Setup(r => r.GetPendingAsync()).ReturnsAsync(new List<EmailImportItem> { item });

            var result = await _service.GetReviewSuggestionsAsync(new[] { 1 });

            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data![0].EmailImportItemId.Should().Be(1);
            result.Data[0].AiTrainingDataId.Should().Be(55);
            result.Data[0].Items.Should().ContainSingle(i => i.Merchant == "Store");
            _textImportService.Verify(t => t.AnalyzeAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetReviewSuggestionsAsync_WhenParsedJsonMissing_FallsBackToAiAnalysis()
        {
            var item = new EmailImportItem
            {
                Id = 2,
                TenantId = TenantId,
                From = "bank@bank.com",
                Subject = "Alerta",
                BodyText = "raw body",
                Status = EmailImportStatus.Pending
            };
            _itemRepo.Setup(r => r.GetPendingAsync()).ReturnsAsync(new List<EmailImportItem> { item });

            var analysis = new TextImportAnalysisDto { AiTrainingDataId = 77 };
            analysis.Items.Add(new ParsedTransactionSuggestionDto { Merchant = "Fallback" });
            _textImportService.Setup(t => t.AnalyzeAsync(It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TextImportAnalysisDto>.Ok(analysis));

            var result = await _service.GetReviewSuggestionsAsync(new[] { 2 });

            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle(s => s.EmailImportItemId == 2 && s.AiTrainingDataId == 77);
            _textImportService.Verify(t => t.AnalyzeAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetReviewSuggestionsAsync_WhenAiFallbackFails_SkipsItemInsteadOfFailingWholeBatch()
        {
            var okItem = new EmailImportItem { Id = 1, TenantId = TenantId, ParsedJson = ValidParsedJson, Status = EmailImportStatus.Pending };
            var failingItem = new EmailImportItem { Id = 2, TenantId = TenantId, Status = EmailImportStatus.Pending };
            _itemRepo.Setup(r => r.GetPendingAsync()).ReturnsAsync(new List<EmailImportItem> { okItem, failingItem });
            _textImportService.Setup(t => t.AnalyzeAsync(It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TextImportAnalysisDto>.Fail("no dice"));

            var result = await _service.GetReviewSuggestionsAsync(new[] { 1, 2 });

            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle(s => s.EmailImportItemId == 1);
        }

        [Fact]
        public async Task GetReviewSuggestionsAsync_WhenEverySelectedItemFails_ReturnsFail()
        {
            var item = new EmailImportItem { Id = 3, TenantId = TenantId, Status = EmailImportStatus.Pending };
            _itemRepo.Setup(r => r.GetPendingAsync()).ReturnsAsync(new List<EmailImportItem> { item });
            _textImportService.Setup(t => t.AnalyzeAsync(It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TextImportAnalysisDto>.Fail("no dice"));

            var result = await _service.GetReviewSuggestionsAsync(new[] { 3 });

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetReviewSuggestionsAsync_WhenRepositoryThrows_ReturnsFail()
        {
            _itemRepo.Setup(r => r.GetPendingAsync()).ThrowsAsync(new Exception("boom"));

            var result = await _service.GetReviewSuggestionsAsync(new[] { 1 });

            result.Success.Should().BeFalse();
        }
    }
}
