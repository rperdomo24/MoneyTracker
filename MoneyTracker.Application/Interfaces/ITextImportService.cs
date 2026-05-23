using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.TextImport;

namespace MoneyTracker.Application.Interfaces
{
    public interface ITextImportService
    {
        Task<OperationResult<TextImportAnalysisDto>> AnalyzeAsync(string rawText);
    }
}
