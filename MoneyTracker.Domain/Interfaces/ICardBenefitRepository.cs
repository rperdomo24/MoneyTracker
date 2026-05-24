using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface ICardBenefitRepository
    {
        Task<List<CardBenefit>> GetAllAsync();
        Task<List<CardBenefit>> GetByAccountAsync(int accountId);
        Task<CardBenefit?> GetByIdAsync(int id);
        Task AddAsync(CardBenefit cardBenefit);
        Task UpdateAsync(CardBenefit cardBenefit);
        Task DeleteAsync(int id);
    }
}
