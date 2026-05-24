using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.CardBenefits;

namespace MoneyTracker.Application.Interfaces
{
    public interface ICardBenefitService
    {
        Task<OperationResult<List<CardBenefitDto>>> GetAllAsync();
        Task<OperationResult<List<CardBenefitDto>>> GetByAccountAsync(int accountId);
        Task<OperationResult<CardBenefitDto>> GetByIdAsync(int id);
        Task<OperationResult<bool>> CreateAsync(CardBenefitDto dto);
        Task<OperationResult<bool>> UpdateAsync(CardBenefitDto dto);
        Task<OperationResult<bool>> DeleteAsync(int id);
    }
}
