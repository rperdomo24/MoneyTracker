using MoneyTracker.Domain.Enums.Account;

namespace MoneyTracker.Application.Interfaces
{
    public interface ISystemCategoryResolver
    {
        Task<int> GetInitialBalanceCategoryIdAsync(bool isIncome, CancellationToken cancellationToken = default);
        Task<int> GetBalanceAdjustmentCategoryIdAsync(bool isIncome, CancellationToken cancellationToken = default);
        Task<(int fromCategoryId, int toCategoryId, string transferTypeName)> GetTransferCategoriesAsync(
            AccountType fromType,
            AccountType toType,
            CancellationToken cancellationToken = default);
    }
}
