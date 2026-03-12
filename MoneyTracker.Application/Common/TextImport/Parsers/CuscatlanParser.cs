using MoneyTracker.Application.DTOs.TextImport;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MoneyTracker.Application.Common.TextImport.Parsers
{
    public sealed class CuscatlanParser : RegexTransactionParserBase
    {
        protected override string ProviderName => "CUSCATLAN";

        protected override Regex Pattern => new(
            @"por\s+USD\s?(?<amount>\d+(\.\d{2})?)\s+en\s+(?<merchant>.+?)\s+el\s+(?<date>\d{4}-\d{2}-\d{2})\s+(?<time>\d{1,2}:\d{2})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected override ParsedTransactionSuggestionDto Map(Match m, string rawText)
        {
            var dto = CreateBaseDto(rawText);

            dto.Amount = decimal.Parse(m.Groups["amount"].Value, CultureInfo.InvariantCulture);
            dto.Merchant = TextImportParsingHelpers.NormalizeMerchant(m.Groups["merchant"].Value);
            dto.Description = dto.Merchant;

            var dateTime = $"{m.Groups["date"].Value} {m.Groups["time"].Value}";
            dto.DateLocal = DateTime.Parse(dateTime, CultureInfo.InvariantCulture);

            return dto;
        }
    }
}
