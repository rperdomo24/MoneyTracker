namespace MoneyTracker.Application.DTOs.TextImport
{
    public sealed class TextImportAnalysisDto
    {
        public List<ParsedTransactionSuggestionDto> Items { get; set; } = new();
        public int TotalDetected => Items.Count;
        public int AiTrainingDataId { get; set; }
    }
}
