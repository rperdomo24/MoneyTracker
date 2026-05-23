using MoneyTracker.Application.DTOs.TextImport;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MoneyTracker.Application.Common.TextImport.Parsers
{
    public sealed class AmexParser : RegexTransactionParserBase
    {
        protected override string ProviderName => "AMEX";

        protected override Regex Pattern => new(
            @"AMEX\s+(?<last4>\d{4}).*?compra\s+por\s+USD\s+(?<amount>\d+(\.\d{2})?)\s+en\s+el\s+comercio:\s+(?<merchant>.+?)\s+(?<date>\d{4}/\d{2}/\d{2})\s+(?<time>\d{2}:\d{2}:\d{2})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected override ParsedTransactionSuggestionDto Map(Match m, string rawText)
        {
            var dto = CreateBaseDto(rawText);

            dto.Amount = decimal.Parse(m.Groups["amount"].Value, CultureInfo.InvariantCulture);
            dto.Merchant = TextImportParsingHelpers.NormalizeMerchant(m.Groups["merchant"].Value);
            dto.Description = dto.Merchant;

            var dateTime = $"{m.Groups["date"].Value} {m.Groups["time"].Value}";
            dto.DateLocal = DateTime.Parse(dateTime, CultureInfo.InvariantCulture);

            dto.AccountHint = m.Groups["last4"].Value;

            return dto;
        }
    }
}
