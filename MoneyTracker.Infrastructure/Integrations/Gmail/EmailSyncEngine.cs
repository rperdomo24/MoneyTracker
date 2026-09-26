using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MoneyTracker.Infrastructure.Integrations.Gmail
{
    public class EmailSyncEngine : IEmailSyncEngine
    {
        // Must match the exact message AiTextImportService.AnalyzeAsync returns when the AI call
        // succeeded but genuinely found no transaction (as opposed to a transient/infra failure).
        private const string NoTransactionFoundMessage = "No recognizable transaction was found.";

        private readonly IGmailConnectionRepository _connectionRepo;
        private readonly IEmailImportRuleRepository _ruleRepo;
        private readonly IEmailImportItemRepository _itemRepo;
        private readonly IGmailApiClient _client;
        private readonly ITextImportService _textImportService;
        private readonly ITokenProtector _protector;
        private readonly GmailSettings _settings;
        private readonly ILogger<EmailSyncEngine> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public EmailSyncEngine(
            IGmailConnectionRepository connectionRepo,
            IEmailImportRuleRepository ruleRepo,
            IEmailImportItemRepository itemRepo,
            IGmailApiClient client,
            ITextImportService textImportService,
            ITokenProtector protector,
            IOptions<GmailSettings> settings,
            ILogger<EmailSyncEngine> logger)
        {
            _connectionRepo = connectionRepo;
            _ruleRepo = ruleRepo;
            _itemRepo = itemRepo;
            _client = client;
            _textImportService = textImportService;
            _protector = protector;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<int> SyncTenantAsync(Guid tenantId)
        {
            var connection = await _connectionRepo.GetActiveByTenantAsync(tenantId);
            if (connection is null) return 0;

            var rules = await _ruleRepo.GetActiveByTenantAsync(tenantId);
            if (rules.Count == 0) return 0;

            string refreshToken;
            try
            {
                refreshToken = _protector.Unprotect(connection.EncryptedRefreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not decrypt Gmail refresh token for tenant {TenantId}", tenantId);
                return 0;
            }

            var query = BuildGmailQuery(rules, connection.LastSyncAtUtc);
            var messageIds = await _client.ListMessageIdsAsync(refreshToken, query, _settings.MaxMessagesPerSync);

            _logger.LogInformation("Gmail sync tenant={TenantId} query='{Query}' matched={Count} message(s)",
                tenantId, query, messageIds.Count);

            var found = 0;
            foreach (var messageId in messageIds)
            {
                try
                {
                    if (await _itemRepo.ExistsByMessageIdAsync(tenantId, messageId))
                        continue;

                    var processed = await ProcessMessageAsync(tenantId, refreshToken, messageId, rules);
                    if (processed) found++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Gmail message {MessageId} for tenant {TenantId}", messageId, tenantId);
                }
            }

            return found;
        }

        private async Task<bool> ProcessMessageAsync(Guid tenantId, string refreshToken, string messageId, List<EmailImportRule> rules)
        {
            var message = await _client.GetMessageAsync(refreshToken, messageId);
            if (message is null)
            {
                _logger.LogWarning("Gmail message {MessageId} could not be fetched", messageId);
                return false;
            }

            var rule = rules.FirstOrDefault(r => message.From.Contains(r.SenderPattern, StringComparison.OrdinalIgnoreCase));
            if (rule is null)
            {
                _logger.LogInformation("Gmail message {MessageId} from '{From}' matched no rule", messageId, message.From);
                return false;
            }

            var item = new EmailImportItem
            {
                TenantId = tenantId,
                GmailMessageId = messageId,
                ThreadId = message.ThreadId,
                From = message.From,
                Subject = message.Subject,
                ReceivedAtUtc = message.ReceivedAtUtc,
                BodyText = message.BodyText,
                BodyHash = ComputeHash(message.BodyText)
            };

            if (IsExcludedBySubject(message.Subject, rule))
            {
                _logger.LogInformation("Gmail message {MessageId} subject '{Subject}' excluded by rule '{Rule}'", messageId, message.Subject, rule.BankLabel);
                item.Status = EmailImportStatus.Ignored;
                await _itemRepo.AddAsync(item);
                return false;
            }

            var aiInput = $"From: {message.From}\nSubject: {message.Subject}\nDate: {message.ReceivedAtUtc:O}\n\n{message.BodyText}";
            var aiResult = await _textImportService.AnalyzeAsync(aiInput);

            if (!aiResult.Success && aiResult.Message != NoTransactionFoundMessage)
            {
                // Transient/infra failure (missing API key, network error, etc.) — don't persist the
                // item, so the message is retried on the next sync instead of being permanently skipped.
                _logger.LogWarning("Gmail message {MessageId} AI analysis failed, will retry next sync: {Message}", messageId, aiResult.Message);
                return false;
            }

            if (!aiResult.Success || aiResult.Data is null || aiResult.Data.Items.Count == 0)
            {
                _logger.LogInformation("Gmail message {MessageId} not recognized as a transaction by AI", messageId);
                item.Status = EmailImportStatus.NotTransaction;
                await _itemRepo.AddAsync(item);
                return false;
            }

            var first = aiResult.Data.Items[0];
            var fingerprint = BuildFingerprint(first.Amount, first.Currency, first.DateLocal, first.AccountHint);

            item.AiTrainingDataId = aiResult.Data.AiTrainingDataId;
            item.Fingerprint = fingerprint;
            item.ParsedJson = JsonSerializer.Serialize(new { items = aiResult.Data.Items }, _jsonOptions);

            if (await _itemRepo.ExistsByFingerprintAsync(tenantId, fingerprint))
            {
                item.Status = EmailImportStatus.Duplicate;
                await _itemRepo.AddAsync(item);
                return false;
            }

            item.Status = EmailImportStatus.Pending;
            await _itemRepo.AddAsync(item);
            return true;
        }

        private static bool IsExcludedBySubject(string? subject, EmailImportRule rule)
        {
            subject ??= string.Empty;

            if (!string.IsNullOrWhiteSpace(rule.SubjectExcludeKeywords))
            {
                var excluded = SplitKeywords(rule.SubjectExcludeKeywords);
                if (excluded.Any(k => subject.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(rule.SubjectIncludeKeywords))
            {
                var included = SplitKeywords(rule.SubjectIncludeKeywords);
                if (included.Count > 0 && !included.Any(k => subject.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        private static List<string> SplitKeywords(string raw) =>
            raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private static string BuildGmailQuery(List<EmailImportRule> rules, DateTime? lastSyncAtUtc)
        {
            var senders = string.Join(" OR ", rules.Select(r => $"from:{r.SenderPattern}"));

            // Gmail's "after:" operator only reliably supports day granularity (YYYY/MM/DD), not epoch
            // seconds — using a raw timestamp silently rounds to a day boundary and can exclude same-day
            // messages. We widen to the whole day and rely on the GmailMessageId dedupe check to skip
            // anything already processed, so day-level precision here is safe.
            var since = (lastSyncAtUtc ?? DateTime.UtcNow.AddDays(-7)).Date;
            return $"({senders}) after:{since:yyyy/MM/dd}";
        }

        private static string BuildFingerprint(decimal amount, string currency, DateTime dateLocal, string accountHint) =>
            $"{amount:F2}|{currency}|{dateLocal:yyyy-MM-dd}|{accountHint}";

        private static string ComputeHash(string text)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
