using System.Text.RegularExpressions;

namespace MoneyTracker.UI.Helpers.TextImport
{
    public static class TextImportNormalizer
    {
        public static string NormalizeSingleItem(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            // Split lines, trim, remove empty lines
            var lines = rawText
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            // If it looks like "Merchant + Date + Amount" across lines, compress it
            // Example: [merchant, 10/02/2026, $2.62]
            if (lines.Count >= 3 &&
                LooksLikeDate(lines[1]) &&
                LooksLikeMoney(lines[2]))
            {
                return $"{lines[0]} {lines[1]} {lines[2]}".Trim();
            }

            // Otherwise, just collapse whitespace/newlines into single spaces
            return Regex.Replace(rawText, @"\s+", " ").Trim();
        }

        private static bool LooksLikeDate(string text)
            => Regex.IsMatch(text, @"^\d{1,2}[/-]\d{1,2}[/-]\d{4}$");

        private static bool LooksLikeMoney(string text)
            => Regex.IsMatch(text, @"^[-+]?\$?\s*\d+(\.\d{1,2})?$");
    }
}
