using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.DTOs.Gmail;
using MoneyTracker.Application.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MoneyTracker.Infrastructure.Integrations.Gmail
{
    public class GmailApiClient : IGmailApiClient
    {
        private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string ApiBase = "https://gmail.googleapis.com/gmail/v1/users/me";
        private const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";

        private readonly HttpClient _http;
        private readonly GmailSettings _settings;
        private readonly string _redirectUri;
        private readonly ILogger<GmailApiClient> _logger;

        public GmailApiClient(
            HttpClient http,
            IOptions<GmailSettings> settings,
            IOptions<ApplicationSettings> applicationSettings,
            ILogger<GmailApiClient> logger)
        {
            _http = http;
            _settings = settings.Value;
            _redirectUri = string.IsNullOrWhiteSpace(_settings.RedirectUri)
                ? $"{applicationSettings.Value.PublicBaseUrl.TrimEnd('/')}/integrations/gmail/callback"
                : _settings.RedirectUri;
            _logger = logger;
        }

        public string BuildAuthorizationUrl(string state)
        {
            const string scope = "https://www.googleapis.com/auth/gmail.readonly https://www.googleapis.com/auth/userinfo.email";

            var query = string.Join("&",
                $"client_id={Uri.EscapeDataString(_settings.ClientId)}",
                $"redirect_uri={Uri.EscapeDataString(_redirectUri)}",
                "response_type=code",
                $"scope={Uri.EscapeDataString(scope)}",
                "access_type=offline",
                "prompt=consent",
                $"state={Uri.EscapeDataString(state)}");

            return $"{AuthEndpoint}?{query}";
        }

        public async Task<(string Email, string RefreshToken)> ExchangeCodeAsync(string code)
        {
            var form = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["redirect_uri"] = _redirectUri,
                ["grant_type"] = "authorization_code"
            };

            using var response = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form));
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gmail token exchange failed {(int)response.StatusCode}: {raw}");

            using var doc = JsonDocument.Parse(raw);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? string.Empty : string.Empty;

            var email = await GetEmailAsync(accessToken);
            return (email, refreshToken);
        }

        public async Task<List<string>> ListMessageIdsAsync(string refreshToken, string query, int maxResults)
        {
            var accessToken = await RefreshAccessTokenAsync(refreshToken);
            var url = $"{ApiBase}/messages?q={Uri.EscapeDataString(query)}&maxResults={maxResults}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gmail messages.list failed {Status}: {Body}", (int)response.StatusCode, errorBody);
                return [];
            }

            var raw = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);

            var ids = new List<string>();
            if (doc.RootElement.TryGetProperty("messages", out var messages))
            {
                foreach (var m in messages.EnumerateArray())
                {
                    var id = m.GetProperty("id").GetString();
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
                }
            }

            return ids;
        }

        public async Task<GmailMessageDto?> GetMessageAsync(string refreshToken, string messageId)
        {
            var accessToken = await RefreshAccessTokenAsync(refreshToken);
            var url = $"{ApiBase}/messages/{messageId}?format=full";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gmail messages.get failed for {Id} {Status}: {Body}", messageId, (int)response.StatusCode, errorBody);
                return null;
            }

            var raw = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var payload = root.GetProperty("payload");
            var headers = payload.TryGetProperty("headers", out var h) ? h : default;

            var from = GetHeader(headers, "From");
            var subject = GetHeader(headers, "Subject");
            var receivedAtUtc = root.TryGetProperty("internalDate", out var idProp) && long.TryParse(idProp.GetString(), out var epochMs)
                ? DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime
                : DateTime.UtcNow;

            var body = ExtractBodyText(payload);

            return new GmailMessageDto
            {
                Id = messageId,
                ThreadId = root.TryGetProperty("threadId", out var tid) ? tid.GetString() : null,
                From = from,
                Subject = subject,
                ReceivedAtUtc = receivedAtUtc,
                BodyText = body
            };
        }

        private async Task<string> GetEmailAsync(string accessToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return string.Empty;

            var raw = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() ?? string.Empty : string.Empty;
        }

        private async Task<string> RefreshAccessTokenAsync(string refreshToken)
        {
            var form = new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["grant_type"] = "refresh_token"
            };

            using var response = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form));
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gmail token refresh failed {(int)response.StatusCode}: {raw}");

            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        }

        private static string GetHeader(JsonElement headers, string name)
        {
            if (headers.ValueKind != JsonValueKind.Array) return string.Empty;
            foreach (var header in headers.EnumerateArray())
            {
                if (header.TryGetProperty("name", out var n) && string.Equals(n.GetString(), name, StringComparison.OrdinalIgnoreCase))
                    return header.TryGetProperty("value", out var v) ? v.GetString() ?? string.Empty : string.Empty;
            }
            return string.Empty;
        }

        private static string ExtractBodyText(JsonElement payload)
        {
            var plain = FindPart(payload, "text/plain");
            if (!string.IsNullOrWhiteSpace(plain)) return DecodeBase64Url(plain);

            var html = FindPart(payload, "text/html");
            if (!string.IsNullOrWhiteSpace(html)) return StripHtml(DecodeBase64Url(html));

            if (payload.TryGetProperty("body", out var body) && body.TryGetProperty("data", out var data))
                return DecodeBase64Url(data.GetString() ?? string.Empty);

            return string.Empty;
        }

        private static string? FindPart(JsonElement payload, string mimeType)
        {
            if (payload.TryGetProperty("mimeType", out var mt) && mt.GetString() == mimeType
                && payload.TryGetProperty("body", out var body) && body.TryGetProperty("data", out var data))
                return data.GetString();

            if (!payload.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var part in parts.EnumerateArray())
            {
                var found = FindPart(part, mimeType);
                if (found is not null) return found;
            }

            return null;
        }

        private static string DecodeBase64Url(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            var s = data.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(s));
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }

        private static string StripHtml(string html)
        {
            // Remove <style>/<script> blocks (content included) before stripping tags — otherwise
            // embedded CSS (e.g. @font-face rules) leaks through as plain text.
            var noStyleOrScript = Regex.Replace(html, @"<(style|script)\b[^>]*>[\s\S]*?</\1>", " ", RegexOptions.IgnoreCase);
            var noTags = Regex.Replace(noStyleOrScript, "<[^>]+>", " ");
            var decoded = System.Net.WebUtility.HtmlDecode(noTags);
            return Regex.Replace(decoded, @"\s+", " ").Trim();
        }
    }
}
