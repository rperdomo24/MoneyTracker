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
    }
}
