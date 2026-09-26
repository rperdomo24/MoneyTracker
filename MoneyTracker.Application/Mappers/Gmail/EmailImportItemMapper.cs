using MoneyTracker.Application.DTOs.Gmail;
using MoneyTracker.Domain.Entities;
using System.Text.Json;

namespace MoneyTracker.Application.Mappers.Gmail
{
    public static class EmailImportItemMapper
    {
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
