using MoneyTracker.Application.DTOs.Gmail;
using MoneyTracker.Application.DTOs.TextImport;
using MoneyTracker.Domain.Entities;
using System.Text.Json;

namespace MoneyTracker.Application.Mappers.Gmail
{
    public static class EmailImportItemMapper
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Same "From/Subject/Date/Body" shape AiTextImportService hashes for caching — must match
        // EmailSyncEngine's aiInput exactly so a review-time fallback call can hit the AI cache.
        public static string BuildAiInput(this EmailImportItem entity) =>
            $"From: {entity.From}\nSubject: {entity.Subject}\nDate: {entity.ReceivedAtUtc:O}\n\n{entity.BodyText}";

        // Reconstructs the parsed transaction suggestions from the JSON persisted at sync time
        // (see EmailSyncEngine.ProcessMessageAsync) so Review doesn't re-call the AI provider.
        // Returns an empty list if ParsedJson is missing or malformed — callers should fall back
        // to a fresh AnalyzeAsync call for that item.
        public static List<ParsedTransactionSuggestionDto> MapToSuggestions(this EmailImportItem entity)
        {
            if (string.IsNullOrWhiteSpace(entity.ParsedJson))
                return new List<ParsedTransactionSuggestionDto>();

            try
            {
                var parsed = JsonSerializer.Deserialize<TextImportAnalysisDto>(entity.ParsedJson, _jsonOptions);
                if (parsed is null || parsed.Items.Count == 0)
                    return new List<ParsedTransactionSuggestionDto>();

                foreach (var item in parsed.Items)
                {
                    item.TempId = Guid.NewGuid();
                    item.RawText = entity.BodyText;
                }

                return parsed.Items;
            }
            catch (JsonException)
            {
                return new List<ParsedTransactionSuggestionDto>();
            }
        }

        public static EmailImportItemDto MapToDto(this EmailImportItem entity)
        {
            var dto = new EmailImportItemDto
            {
                Id = entity.Id,
                From = entity.From,
                Subject = entity.Subject,
                ReceivedAtUtc = entity.ReceivedAtUtc,
                BodyText = entity.BodyText,
                Status = entity.Status,
                TransactionId = entity.TransactionId
            };

            if (string.IsNullOrWhiteSpace(entity.ParsedJson))
                return dto;

            try
            {
                using var doc = JsonDocument.Parse(entity.ParsedJson);
                if (doc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    var first = items[0];
                    if (first.TryGetProperty("amount", out var amount))
                        dto.Amount = amount.GetDecimal();
                    if (first.TryGetProperty("provider", out var provider))
                        dto.Provider = provider.GetString();
                }
            }
            catch (JsonException)
            {
                // Leave preview fields empty if the stored JSON is malformed.
            }

            return dto;
        }
    }
}
