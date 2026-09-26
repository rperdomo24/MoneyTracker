using MoneyTracker.Application.DTOs.Gmail;

namespace MoneyTracker.Application.Interfaces
{
    public interface IGmailApiClient
    {
        string BuildAuthorizationUrl(string state);

        /// <summary>Exchanges an OAuth authorization code for tokens. Returns (email, refreshToken).</summary>
        Task<(string Email, string RefreshToken)> ExchangeCodeAsync(string code);

        /// <summary>Lists message ids matching the given Gmail search query (server-side filter).</summary>
        Task<List<string>> ListMessageIdsAsync(string refreshToken, string query, int maxResults);

        Task<GmailMessageDto?> GetMessageAsync(string refreshToken, string messageId);
    }
}
