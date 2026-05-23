using MoneyTracker.Application.DTOs.TextImport;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MoneyTracker.Application.Common.TextImport
{
    public static class TextImportParsingHelpers
    {
        public static string Normalize(string text)
            => Regex.Replace(text.Trim(), @"\s+", " ");

        public static string NormalizeMerchant(string merchant)
        {
            merchant = merchant.Trim();

            // conservative cleaning
            merchant = merchant.Replace("GOOGLE *", "GOOGLE ", StringComparison.OrdinalIgnoreCase);

            return Regex.Replace(merchant, @"\s+", " ")
                         .Trim()
                         .ToUpperInvariant();
        }

        public static decimal ParseDecimal(string s)
            => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

        public static string? ExtractFirstLast4(string text)
        {
            var m = Regex.Match(text, @"\b(\d{4})\b");
            return m.Success ? m.Groups[1].Value : null;
        }

        public static void Score(
            ParsedTransactionSuggestionDto dto,
            bool hasAmount,
            bool hasDate,
            bool hasMerchant,
            bool hasAccountHint)
        {
            decimal score = 0m;

            if (hasAmount) score += 0.35m; else dto.Warnings.Add("Amount not detected.");
            if (hasDate) score += 0.25m; else dto.Warnings.Add("Date/time not detected.");
            if (hasMerchant) score += 0.25m; else dto.Warnings.Add("Merchant not detected.");
            if (hasAccountHint) score += 0.15m; else dto.Warnings.Add("Account hint not detected.");

            dto.Confidence = score;
        }
    }
}