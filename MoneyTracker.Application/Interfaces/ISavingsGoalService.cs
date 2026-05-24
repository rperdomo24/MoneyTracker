using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Goals;

namespace MoneyTracker.Application.Interfaces
{
    public interface ISavingsGoalService
    {
        Task<OperationResult<List<SavingsGoalDto>>> GetAllAsync();
        Task<OperationResult<SavingsGoalDto>> GetByIdAsync(int id);
        Task<OperationResult> CreateAsync(SavingsGoalDto dto);
        Task<OperationResult> UpdateAsync(SavingsGoalDto dto);
        Task<OperationResult> DeleteAsync(int id);
        Task<OperationResult> AddContributionAsync(SavingsContributionDto dto);
        Task<OperationResult> DeleteContributionAsync(int contributionId);
        Task<OperationResult> ToggleCompletedAsync(int id);
    }
}
