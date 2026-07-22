using MoneyTracker.Application.Common;
using MoneyTracker.Application.Common.TextImport;
using MoneyTracker.Application.Common.TextImport.Parsers;
using MoneyTracker.Application.DTOs.TextImport;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Enums.Ai;

namespace MoneyTracker.Application.Services
{
    public sealed class TextImportService : ITextImportService
    {
        // Phase 1: single-item analysis (order matters)
        private readonly List<ITextTransactionParser> _parsers = new()
        {
            new SingleLineParser(),  // ANDA PAGO AUTOMATICO ZONA 10/02/2026 $2.62
            new CuscatlanParser(),   // "Alerta de compra B.CUSCATLAN..."
            new GenericPurchaseParser(),
            new AmexParser()         // "Alertas PRF AMEX 0175..."
        };

        public Task<OperationResult<TextImportAnalysisDto>> AnalyzeAsync(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return Task.FromResult(OperationResult<TextImportAnalysisDto>.Fail("Text is empty."));

            // Keep it consistent everywhere
            var cleaned = TextImportParsingHelpers.Normalize(rawText);

            ParsedTransactionSuggestionDto? item = null;

            foreach (var parser in _parsers)
            {
                item = parser.TryParse(cleaned);
                if (item is not null)
                    break;
            }

            if (item is null)
                return Task.FromResult(OperationResult<TextImportAnalysisDto>.Fail("No recognizable transaction was found."));

            var dto = new TextImportAnalysisDto();
            dto.Items.Add(item);

            return Task.FromResult(OperationResult<TextImportAnalysisDto>.Ok(dto, "Text analyzed successfully."));
        }

        public Task UpdateTrainingFeedbackAsync(int trainingDataId, AiUserFeedback feedback)
            => Task.CompletedTask;
    }
}
