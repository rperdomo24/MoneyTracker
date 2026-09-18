using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.TextImport;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Ai;
using MoneyTracker.Domain.Interfaces;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MoneyTracker.Application.Services
{
    public sealed class AiTextImportService : ITextImportService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly IAiCallLogRepository _callLogRepo;
        private readonly IAiTextImportCacheRepository _importCacheRepo;
        private readonly IAiTrainingDataRepository _trainingRepo;
        private readonly ILogger<AiTextImportService> _logger;

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

        public AiTextImportService(
            HttpClient http,
            IConfiguration config,
            IAiCallLogRepository callLogRepo,
            IAiTextImportCacheRepository importCacheRepo,
            IAiTrainingDataRepository trainingRepo,
            ILogger<AiTextImportService> logger)
        {
            _http = http;
            _config = config;
            _callLogRepo = callLogRepo;
            _importCacheRepo = importCacheRepo;
            _trainingRepo = trainingRepo;
            _logger = logger;
        }

        public async Task<OperationResult<TextImportAnalysisDto>> AnalyzeAsync(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return OperationResult<TextImportAnalysisDto>.Fail("Text is empty.");

            try
            {
                var inputHash = ComputeHash(rawText);
                var cached = await _importCacheRepo.GetByHashAsync(inputHash);
                if (cached is not null)
                {
                    var cachedResult = ParseAiResponse(cached.Content);
                    if (cachedResult is null)
                        _logger.LogWarning("AI cache hit but JSON was invalid/truncated for hash={Hash}. Falling through to fresh call.", inputHash[..8]);
                    else if (cachedResult.Items.Count > 0)
                    {
                        var cachedModel = _config["GoogleAiSettings:Model"] ?? "gemini";
                        _ = _callLogRepo.AddAsync(new AiCallLog
                        {
                            Service = "TextImport",
                            Model = cachedModel,
                            WasCacheHit = true,
                            CalledAtUtc = DateTime.UtcNow
                        });
                        cachedResult.AiTrainingDataId = await _trainingRepo.AddAsync(new AiTrainingData
                        {
                            ServiceType = AiServiceType.TextImport,
                            Model = cachedModel,
                            InputType = AiInputType.SmsText,
                            RawInput = rawText,
                            OutputType = AiOutputType.ParsedTransactionsJson,
                            RawOutput = cached.Content,
                            CalledAtUtc = DateTime.UtcNow
                        });
                        _logger.LogInformation("AI call [TextImport] cache hit hash={Hash}", inputHash[..8]);
                        return OperationResult<TextImportAnalysisDto>.Ok(cachedResult, "Text analyzed successfully.");
                    }
                }

                var (jsonResponse, inputTokens, outputTokens) = await CallGoogleAsync(rawText, inputHash);

                if (jsonResponse is null)
                    return OperationResult<TextImportAnalysisDto>.Fail("AI provider returned no response.");

                var result = ParseAiResponse(jsonResponse);

                if (result is null || result.Items.Count == 0)
                    return OperationResult<TextImportAnalysisDto>.Fail("No recognizable transaction was found.");

                var model = _config["GoogleAiSettings:Model"] ?? "gemini";
                result.AiTrainingDataId = await _trainingRepo.AddAsync(new AiTrainingData
                {
                    ServiceType = AiServiceType.TextImport,
                    Model = model,
                    InputType = AiInputType.SmsText,
                    RawInput = rawText,
                    OutputType = AiOutputType.ParsedTransactionsJson,
                    RawOutput = jsonResponse,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    CalledAtUtc = DateTime.UtcNow
                });

                return OperationResult<TextImportAnalysisDto>.Ok(result, "Text analyzed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI text import failed [AnalyzeAsync]");
                return OperationResult<TextImportAnalysisDto>.Fail("AI analysis failed. Check logs for details.");
            }
        }

        private static string ComputeHash(string text)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim()));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private async Task<(string? text, int inputTokens, int outputTokens)> CallGoogleAsync(string rawText, string inputHash)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
                    generationConfig = new { maxOutputTokens = maxTokens, temperature = 0, responseMimeType = "application/json", thinkingConfig = new { thinkingBudget = 0 } }
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
                    generationConfig = new { maxOutputTokens = maxTokens, temperature = 0, responseMimeType = "application/json", thinkingConfig = new { thinkingBudget = 0 } }
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
            sw.Stop();
            var doc = JsonNode.Parse(raw);

            var text = doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();

            var inputTokens = doc?["usageMetadata"]?["promptTokenCount"]?.GetValue<int>() ?? 0;
            var outputTokens = doc?["usageMetadata"]?["candidatesTokenCount"]?.GetValue<int>() ?? 0;
            var cachedTokens = doc?["usageMetadata"]?["cachedContentTokenCount"]?.GetValue<int>() ?? 0;

            _ = _callLogRepo.AddAsync(new AiCallLog
            {
                Service = "TextImport",
                Model = model,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                CachedTokens = cachedTokens,
                WasCacheHit = false,
                DurationMs = sw.ElapsedMilliseconds,
                CalledAtUtc = DateTime.UtcNow
            });

            _logger.LogInformation("AI call [TextImport] model={Model} in={Input} out={Output} cached={Cached} ms={Ms}",
                model, inputTokens, outputTokens, cachedTokens, sw.ElapsedMilliseconds);

            if (text is not null && IsValidJson(text))
                await _importCacheRepo.SaveAsync(inputHash, text);

            return (text, inputTokens, outputTokens);
        }

        public async Task UpdateTrainingFeedbackAsync(int trainingDataId, AiUserFeedback feedback)
        {
            if (trainingDataId <= 0) return;
            await _trainingRepo.UpdateFeedbackAsync(trainingDataId, feedback);
        }

        private static bool IsValidJson(string text)
        {
            try { using var _ = JsonDocument.Parse(text); return true; }
            catch (JsonException) { return false; }
        }

        private static TextImportAnalysisDto? ParseAiResponse(string jsonText)
        {
            try
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
            catch (JsonException)
            {
                return null;
            }
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
