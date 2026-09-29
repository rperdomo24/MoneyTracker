using MoneyTracker.Application.DTOs.TextImport;

namespace MoneyTracker.Application.DTOs.Gmail
{
    public class EmailReviewSuggestionDto
    {
        public int EmailImportItemId { get; set; }
        public int AiTrainingDataId { get; set; }
        public List<ParsedTransactionSuggestionDto> Items { get; set; } = new();
    }
}
