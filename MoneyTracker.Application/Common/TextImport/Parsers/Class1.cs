using MoneyTracker.Application.DTOs.TextImport;
using MoneyTracker.Domain.Enums.Transaction;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MoneyTracker.Application.Common.TextImport.Parsers
{
    public sealed class GenericPurchaseParser : RegexTransactionParserBase
    {
        protected override string ProviderName => "GENERIC";

        // Examples:
        // "Compra realizada el 2026-02-10 por USD 15.00 en Walmart"
        // "Compra el 2026-02-10 por USD15.00 en Walmart"
        protected override Regex Pattern => new(
            @"\bel\s+(?<date>\d{4}-\d{2}-\d{2})\s+por\s+USD\s?(?<amount>\d+(\.\d{1,2})?)\s+en\s+(?<merchant>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected override ParsedTransactionSuggestionDto Map(Match m, string rawText)
        {
            var dto = CreateBaseDto(rawText);

            dto.Type = TransactionTypeEnum.Expense;
            dto.Currency = "USD";

            dto.Amount = decimal.TryParse(m.Groups["amount"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
                ? v
                : 0m;

            dto.Merchant = TextImportParsingHelpers.NormalizeMerchant(m.Groups["merchant"].Value);
            dto.Description = dto.Merchant;

            // date only (no time) -> set 00:00 local
            var dateStr = m.Groups["date"].Value;
            dto.DateLocal = DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d
                : DateTime.Today;

            return dto;
        }
    }
}
