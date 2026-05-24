using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface ISavingsGoalRepository
    {
        Task<List<SavingsGoal>> GetAllAsync();
        Task<SavingsGoal?> GetByIdAsync(int id);
        Task AddAsync(SavingsGoal goal);
        Task UpdateAsync(SavingsGoal goal);
        Task SoftDeleteAsync(int id);
        Task AddContributionAsync(SavingsContribution contribution);
        Task DeleteContributionAsync(int contributionId);
    }
}
