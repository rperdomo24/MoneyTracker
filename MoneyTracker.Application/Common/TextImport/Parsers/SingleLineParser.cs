using MoneyTracker.Application.DTOs.TextImport;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MoneyTracker.Application.Common.TextImport.Parsers
{
    public sealed class SingleLineParser : RegexTransactionParserBase
    {
        protected override string ProviderName => "APP_ROW";

        protected override Regex Pattern => new(
            @"^(?<merchant>.+?)\s+(?<date>\d{1,2}[/-]\d{1,2}[/-]\d{4})\s+\$?(?<amount>\d+(\.\d{1,2})?)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected override ParsedTransactionSuggestionDto Map(Match m, string rawText)
        {
            var dto = CreateBaseDto(rawText);

            dto.Merchant = TextImportParsingHelpers.NormalizeMerchant(m.Groups["merchant"].Value);
            dto.Description = dto.Merchant;

            dto.Amount = decimal.Parse(m.Groups["amount"].Value, CultureInfo.InvariantCulture);

            var dateStr = m.Groups["date"].Value;
            dto.DateLocal = DateTime.Parse(dateStr, CultureInfo.InvariantCulture);

            return dto;
        }
    }
}
