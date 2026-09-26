using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Gmail;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers.Gmail;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class EmailImportService : IEmailImportService
    {
        private readonly IGmailConnectionRepository _connectionRepo;
        private readonly IEmailImportRuleRepository _ruleRepo;
        private readonly IEmailImportItemRepository _itemRepo;
        private readonly IGmailApiClient _gmailClient;
        private readonly IEmailSyncEngine _syncEngine;
        private readonly IEmailSyncScheduler _scheduler;
        private readonly ITenantContext _tenantContext;
        private readonly ITokenProtector _protector;
        private readonly ILogger<EmailImportService> _logger;

        public EmailImportService(
            IGmailConnectionRepository connectionRepo,
            IEmailImportRuleRepository ruleRepo,
            IEmailImportItemRepository itemRepo,
            IGmailApiClient gmailClient,
            IEmailSyncEngine syncEngine,
            IEmailSyncScheduler scheduler,
            ITenantContext tenantContext,
            ITokenProtector protector,
            ILogger<EmailImportService> logger)
        {
            _connectionRepo = connectionRepo;
            _ruleRepo = ruleRepo;
            _itemRepo = itemRepo;
            _gmailClient = gmailClient;
            _syncEngine = syncEngine;
            _scheduler = scheduler;
            _tenantContext = tenantContext;
            _protector = protector;
            _logger = logger;
        }

        public async Task<OperationResult<GmailConnectionDto?>> GetConnectionAsync()
        {
            try
            {
                var connection = await _connectionRepo.GetActiveAsync();
                return OperationResult<GmailConnectionDto?>.Ok(connection?.MapToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Gmail connection");
                return OperationResult<GmailConnectionDto?>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> CompleteConnectionAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return OperationResult.Fail("Missing authorization code.");

            try
            {
                var (email, refreshToken) = await _gmailClient.ExchangeCodeAsync(code);
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return OperationResult.Fail("Google did not return a refresh token. Revoke access at myaccount.google.com/permissions and try again.");

                var existing = await _connectionRepo.GetActiveAsync();
                if (existing is not null)
                {
                    existing.Email = email;
                    existing.EncryptedRefreshToken = _protector.Protect(refreshToken);
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _connectionRepo.UpdateAsync(existing);
                }
                else
                {
                    await _connectionRepo.AddAsync(new GmailConnection
                    {
                        Email = email,
                        EncryptedRefreshToken = _protector.Protect(refreshToken)
                    });
                }

                return OperationResult.Ok("Gmail connected successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing Gmail OAuth connection");
                return OperationResult.Fail("Could not connect to Gmail. Check logs for details.");
            }
        }

        public async Task<OperationResult> DisconnectAsync()
        {
            try
            {
                var connection = await _connectionRepo.GetActiveAsync();
                if (connection is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                if (_tenantContext.TenantId.HasValue)
                    _scheduler.DisableAutoSync(_tenantContext.TenantId.Value);

                await _connectionRepo.DeleteAsync(connection.Id);
                return OperationResult.Ok(OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting Gmail");
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> SetAutoSyncAsync(bool enabled, int intervalMinutes)
        {
            if (enabled && intervalMinutes < 15)
                return OperationResult.Fail("Auto-sync interval must be at least 15 minutes.");

            try
            {
                var connection = await _connectionRepo.GetActiveAsync();
                if (connection is null)
                    return OperationResult.Fail("Connect a Gmail account first.");

                connection.AutoSyncEnabled = enabled;
                connection.SyncIntervalMinutes = intervalMinutes;
                connection.UpdatedAt = DateTime.UtcNow;
                await _connectionRepo.UpdateAsync(connection);

                if (!_tenantContext.TenantId.HasValue)
                    return OperationResult.Fail("Tenant is required.");

                if (enabled)
                    _scheduler.EnableAutoSync(_tenantContext.TenantId.Value, intervalMinutes);
                else
                    _scheduler.DisableAutoSync(_tenantContext.TenantId.Value);

                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Gmail auto-sync setting");
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<List<EmailImportRuleDto>>> GetRulesAsync()
        {
            try
            {
                var rules = await _ruleRepo.GetAllAsync();
                return OperationResult<List<EmailImportRuleDto>>.Ok(rules.Select(r => r.MapToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching email import rules");
                return OperationResult<List<EmailImportRuleDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> SaveRuleAsync(EmailImportRuleDto dto)
        {
            try
            {
                if (dto.Id > 0)
                {
                    var entity = await _ruleRepo.GetByIdAsync(dto.Id);
                    if (entity is null)
                        return OperationResult.Fail(OperationMessages.NotFound);

                    entity.UpdateEntity(dto);
                    await _ruleRepo.UpdateAsync(entity);
                    return OperationResult.Ok(OperationMessages.Updated);
                }

                await _ruleRepo.AddAsync(dto.MapToEntity());
                return OperationResult.Ok(OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving email import rule");
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> DeleteRuleAsync(int id)
        {
            try
            {
                var entity = await _ruleRepo.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                await _ruleRepo.DeleteAsync(id);
                return OperationResult.Ok(OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email import rule {Id}", id);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<int>> SyncNowAsync()
        {
            if (!_tenantContext.TenantId.HasValue)
                return OperationResult<int>.Fail("Tenant is required.");

            try
            {
                var connection = await _connectionRepo.GetActiveAsync();
                if (connection is null)
                    return OperationResult<int>.Fail("Connect a Gmail account first.");

                var found = await _syncEngine.SyncTenantAsync(_tenantContext.TenantId.Value);

                connection.LastSyncAtUtc = DateTime.UtcNow;
                await _connectionRepo.UpdateAsync(connection);

                return OperationResult<int>.Ok(found, found == 0
                    ? "No new transaction emails found."
                    : $"{found} new email(s) found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running Gmail sync");
                return OperationResult<int>.Fail("Gmail sync failed. Check logs for details.");
            }
        }

        public async Task<OperationResult<List<EmailImportItemDto>>> GetPendingAsync()
        {
            try
            {
                var items = await _itemRepo.GetPendingAsync();
                return OperationResult<List<EmailImportItemDto>>.Ok(items.Select(i => i.MapToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending email import items");
                return OperationResult<List<EmailImportItemDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> MarkImportedAsync(int itemId)
        {
            try
            {
                var item = await _itemRepo.GetByIdAsync(itemId);
                if (item is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                item.Status = EmailImportStatus.Imported;
                item.UpdatedAt = DateTime.UtcNow;
                await _itemRepo.UpdateAsync(item);

                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email import item {Id} as imported", itemId);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> DismissAsync(int itemId)
        {
            try
            {
                var item = await _itemRepo.GetByIdAsync(itemId);
                if (item is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                item.Status = EmailImportStatus.Dismissed;
                item.UpdatedAt = DateTime.UtcNow;
                await _itemRepo.UpdateAsync(item);

                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing email import item {Id}", itemId);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
