using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.TextImport;
using MoneyTracker.Application.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MoneyTracker.Application.Services
{
    public sealed class AiTextImportService : ITextImportService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private const string SystemPrompt = """
            You are a financial transaction parser for a personal finance app.
            Extract ALL financial transactions from the bank SMS or notification text provided.

            CLASSIFICATION RULES:
            - "Debito", "compra", "Alerta de compra", "realizaste una transferencia", "transferencia con tu Cuenta" → type: "Expense"
            - "Credito", "Abono", "ha recibido", "recibido un abono", "DEP.EFECTIVO", "Has recibido" → type: "Income"
            - "Transfer365 a Cuenta De Ahorro a nombre de" (sending to another person) → type: "Transfer"
            - Ignore OTP codes, security tokens, login codes ("codigo", "token para", "iniciar sesion"), and promotional messages.

            EXTRACTION RULES:
            - amount: numeric decimal, no currency symbols. If amount has comma as thousands separator (e.g. $5,401.00), parse correctly as 5401.00
            - currency: always "USD" unless explicitly stated otherwise
            - dateLocal: ISO 8601 "yyyy-MM-ddTHH:mm:ss". Formats seen: "2026-05-23 12:13", "23/05/2026 12:21:42 A.M.", "2026-05-16", "08/05/26 15:11:53", "20/05/26 15:19:34", "2026/05/08 12:48:22". If only date, append T00:00:00.
            - merchant: store name, sender name, or concept. For debits with no merchant, use the bank name. For incoming transfers, use sender full name.
            - description: 1 short sentence describing the transaction.
            - provider: detected bank name. Examples: "CUSCATLAN", "NIU", "DAVIVIENDA", "PRF", "AMEX", "VISA".
            - accountHint: last 4 digits of card or account number visible in text (e.g. "0488", "9511", "2603"). Empty string if none visible.
            - confidence: 0.0–1.0. High (0.9+) when amount+date+type all clear. Lower when fields missing.
            - warnings: list field names you could NOT extract. Empty array if all extracted.

            MULTI-LINE FORMAT (Davivienda style):
            Messages with "Cta:", "Fec:", "Monto:" fields — parse Fec as date, Monto as amount, Concep as merchant/description.

            If no financial transaction found, return { "items": [] }.
            Return ONLY valid JSON. No markdown. No explanation. No code blocks.

            JSON schema:
            {
              "items": [
                {
                  "type": "Expense|Income|Transfer",
                  "amount": 0.00,
                  "currency": "USD",
                  "dateLocal": "2026-05-23T12:13:00",
                  "merchant": "merchant or sender/receiver name",
                  "description": "brief description",
                  "provider": "BANKNAME",
                  "accountHint": "1234",
                  "confidence": 0.95,
                  "warnings": []
                }
              ]
            }
            """;

        public AiTextImportService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<OperationResult<TextImportAnalysisDto>> AnalyzeAsync(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return OperationResult<TextImportAnalysisDto>.Fail("Text is empty.");

            try
            {
                var jsonResponse = await CallGoogleAsync(rawText);

                if (jsonResponse is null)
                    return OperationResult<TextImportAnalysisDto>.Fail("AI provider returned no response.");

                var result = ParseAiResponse(jsonResponse);

                if (result is null || result.Items.Count == 0)
                    return OperationResult<TextImportAnalysisDto>.Fail("No recognizable transaction was found.");

                return OperationResult<TextImportAnalysisDto>.Ok(result, "Text analyzed successfully.");
            }
            catch (Exception ex)
            {
                return OperationResult<TextImportAnalysisDto>.Fail($"AI analysis failed: {ex.Message}");
            }
        }

        private async Task<string?> CallGoogleAsync(string rawText)
        {
            var model = _config["GoogleAiSettings:Model"] ?? "gemini-1.5-flash";
            var maxTokens = int.TryParse(_config["GoogleAiSettings:MaxTokens"], out var mt) ? mt : 1024;
            var projectId = _config["GoogleAiSettings:ProjectId"];
            var useVertexAi = !string.IsNullOrWhiteSpace(projectId);

            string url;
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "");

            if (useVertexAi)
            {
                var location = _config["GoogleAiSettings:Location"] ?? "us-central1";
                var apiHost = location == "global" ? "aiplatform.googleapis.com" : $"{location}-aiplatform.googleapis.com";
                url = $"https://{apiHost}/v1/projects/{projectId}/locations/{location}/publishers/google/models/{model}:generateContent";

                var saPath = _config["GoogleAiSettings:ServiceAccountJsonPath"];
                GoogleCredential credential = !string.IsNullOrWhiteSpace(saPath) && File.Exists(saPath)
                    ? GoogleCredential.FromFile(saPath).CreateScoped("https://www.googleapis.com/auth/cloud-platform")
                    : (await GoogleCredential.GetApplicationDefaultAsync()).CreateScoped("https://www.googleapis.com/auth/cloud-platform");

                var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var vertexBody = new
                {
                    systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
                    contents = new[] { new { role = "user", parts = new[] { new { text = rawText } } } },
                    generationConfig = new { maxOutputTokens = maxTokens, temperature = 0, responseMimeType = "application/json" }
                };
                requestMessage.RequestUri = new Uri(url);
                requestMessage.Content = new StringContent(JsonSerializer.Serialize(vertexBody), Encoding.UTF8, "application/json");
            }
            else
            {
                var apiKey = _config["GoogleAiSettings:ApiKey"];
                url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var studioBody = new
                {
                    system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
                    contents = new[] { new { parts = new[] { new { text = rawText } } } },
                    generationConfig = new { maxOutputTokens = maxTokens, temperature = 0, responseMimeType = "application/json" }
                };
                requestMessage.RequestUri = new Uri(url);
                requestMessage.Content = new StringContent(JsonSerializer.Serialize(studioBody), Encoding.UTF8, "application/json");
            }

            var response = await _http.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Google AI {(int)response.StatusCode}: {errorBody}");
            }

            var raw = await response.Content.ReadAsStringAsync();
            var doc = JsonNode.Parse(raw);
            return doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        }

        private static TextImportAnalysisDto? ParseAiResponse(string jsonText)
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            if (!root.TryGetProperty("items", out var itemsEl))
                return null;

            var dto = new TextImportAnalysisDto();

            foreach (var el in itemsEl.EnumerateArray())
            {
                var item = new ParsedTransactionSuggestionDto
                {
                    Type = ParseType(el),
                    Amount = el.TryGetProperty("amount", out var a) ? a.GetDecimal() : 0m,
                    Currency = el.TryGetProperty("currency", out var c) ? c.GetString() ?? "USD" : "USD",
                    DateLocal = ParseDate(el),
                    Merchant = el.TryGetProperty("merchant", out var m) ? m.GetString() ?? string.Empty : string.Empty,
                    Description = el.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty,
                    Provider = el.TryGetProperty("provider", out var p) ? p.GetString() ?? string.Empty : string.Empty,
                    AccountHint = el.TryGetProperty("accountHint", out var ah) ? ah.GetString() ?? string.Empty : string.Empty,
                    Confidence = el.TryGetProperty("confidence", out var cf) ? cf.GetDecimal() : 0m,
                    Warnings = ParseWarnings(el)
                };

                dto.Items.Add(item);
            }

            return dto;
        }

        private static Domain.Enums.Transaction.TransactionTypeEnum ParseType(JsonElement el)
        {
            if (!el.TryGetProperty("type", out var t)) return Domain.Enums.Transaction.TransactionTypeEnum.Expense;
            return t.GetString() switch
            {
                "Income" => Domain.Enums.Transaction.TransactionTypeEnum.Income,
                "Transfer" => Domain.Enums.Transaction.TransactionTypeEnum.Transfer,
                _ => Domain.Enums.Transaction.TransactionTypeEnum.Expense
            };
        }

        private static DateTime ParseDate(JsonElement el)
        {
            if (!el.TryGetProperty("dateLocal", out var d)) return DateTime.Today;
            return DateTime.TryParse(d.GetString(), out var dt) ? dt : DateTime.Today;
        }

        private static List<string> ParseWarnings(JsonElement el)
        {
            var list = new List<string>();
            if (!el.TryGetProperty("warnings", out var w)) return list;
            foreach (var warn in w.EnumerateArray())
            {
                var s = warn.GetString();
                if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
            }
            return list;
        }
    }
}
