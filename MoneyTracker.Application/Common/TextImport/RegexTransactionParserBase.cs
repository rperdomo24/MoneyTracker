using MoneyTracker.Application.DTOs.TextImport;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Enums.Transaction;
using System.Text.RegularExpressions;

namespace MoneyTracker.Application.Common.TextImport
{
    public abstract class RegexTransactionParserBase : ITextTransactionParser
    {
        protected abstract Regex Pattern { get; }
        protected abstract string ProviderName { get; }

        protected abstract ParsedTransactionSuggestionDto Map(Match match, string rawText);

        public ParsedTransactionSuggestionDto? TryParse(string text)
        {
            var match = Pattern.Match(text);
            if (!match.Success)
                return null;

            var dto = Map(match, text);

            // Fallback AccountHint
            if (string.IsNullOrWhiteSpace(dto.AccountHint))
                dto.AccountHint = TextImportParsingHelpers.ExtractFirstLast4(text) ?? string.Empty;

            // Score unified
            TextImportParsingHelpers.Score(dto,
                hasAmount: dto.Amount > 0,
                hasDate: dto.DateLocal != default,
                hasMerchant: !string.IsNullOrWhiteSpace(dto.Merchant),
                hasAccountHint: !string.IsNullOrWhiteSpace(dto.AccountHint));

            return dto;
        }

        protected ParsedTransactionSuggestionDto CreateBaseDto(string rawText)
            => new()
            {
                Provider = ProviderName,
                Currency = "USD",
                Type = TransactionTypeEnum.Expense,
                RawText = rawText
            };
    }
}
