using MoneyTracker.Application.DTOs.TextImport;

namespace MoneyTracker.Application.Interfaces
{
    public interface ITextTransactionParser
    {
        ParsedTransactionSuggestionDto? TryParse(string text);
    }
}
