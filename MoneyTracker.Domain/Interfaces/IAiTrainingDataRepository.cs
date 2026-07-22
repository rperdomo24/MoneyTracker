using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Ai;

namespace MoneyTracker.Domain.Interfaces
{
    public interface IAiTrainingDataRepository
    {
        Task<int> AddAsync(AiTrainingData data);
        Task UpdateFeedbackAsync(int id, AiUserFeedback feedback);
    }
}
