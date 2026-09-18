using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<OperationResult<List<CategoryDto>>> GetAllAsync();
        Task<OperationResult<CategoryDto?>> GetByIdAsync(int id);
        Task<OperationResult<bool>> CreateAsync(CategoryDto dto);
        Task<OperationResult<bool>> UpdateAsync(CategoryDto dto);
        Task<OperationResult<bool>> DeleteAsync(int id);
        Task<OperationResult<List<CategoryDto>>> GetAllWithChildAsync(bool incluideSystem = true);
        Task<OperationResult<bool>> MergeAsync(int sourceId, int targetId, List<int>? transactionIdsToMove = null);
        Task<OperationResult<CategoryMergePreviewDto>> GetMergePreviewAsync(int sourceId, int targetId);
    }
}
