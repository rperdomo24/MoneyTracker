using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Gmail;

namespace MoneyTracker.Application.Interfaces
{
    public interface IEmailImportService
    {
        Task<OperationResult<GmailConnectionDto?>> GetConnectionAsync();
        Task<OperationResult> CompleteConnectionAsync(string code);
        Task<OperationResult> DisconnectAsync();
        Task<OperationResult> SetAutoSyncAsync(bool enabled, int intervalMinutes);

        Task<OperationResult<List<EmailImportRuleDto>>> GetRulesAsync();
        Task<OperationResult> SaveRuleAsync(EmailImportRuleDto dto);
        Task<OperationResult> DeleteRuleAsync(int id);

        Task<OperationResult<int>> SyncNowAsync();
        Task<OperationResult<List<EmailImportItemDto>>> GetPendingAsync();
        Task<OperationResult<List<EmailReviewSuggestionDto>>> GetReviewSuggestionsAsync(IReadOnlyCollection<int> itemIds);
        Task<OperationResult> MarkImportedAsync(int itemId);
        Task<OperationResult> DismissAsync(int itemId);
    }
}
