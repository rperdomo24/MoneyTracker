using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Merchants;

namespace MoneyTracker.Application.Interfaces
{
    public interface IMerchantService
    {
        Task<OperationResult<List<MerchantDto>>> GetAllAsync();
        Task<OperationResult<MerchantDto>> GetByIdAsync(int id);
        Task<OperationResult<bool>> CreateAsync(MerchantDto dto);
        Task<OperationResult<bool>> UpdateAsync(MerchantDto dto);
        Task<OperationResult<bool>> DeleteAsync(int id);
    }
}
