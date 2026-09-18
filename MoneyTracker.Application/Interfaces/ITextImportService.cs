using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.TextImport;
using MoneyTracker.Domain.Enums.Ai;

namespace MoneyTracker.Application.Interfaces
{
    public interface ITextImportService
    {
        Task<OperationResult<TextImportAnalysisDto>> AnalyzeAsync(string rawText);
        Task UpdateTrainingFeedbackAsync(int trainingDataId, AiUserFeedback feedback);
    }
}
