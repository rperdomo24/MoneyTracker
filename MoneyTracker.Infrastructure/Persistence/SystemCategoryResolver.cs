using Microsoft.EntityFrameworkCore;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Enums.Account;

namespace MoneyTracker.Infrastructure.Persistence
{
    public class SystemCategoryResolver : ISystemCategoryResolver
    {
        private readonly MoneyTrackerDbContext _dbContext;

        public SystemCategoryResolver(MoneyTrackerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<int> GetInitialBalanceCategoryIdAsync(bool isIncome, CancellationToken cancellationToken = default)
            => GetIdByCodeAsync(SystemCategoryCodes.GetInitialBalanceCode(isIncome), cancellationToken);

        public Task<int> GetBalanceAdjustmentCategoryIdAsync(bool isIncome, CancellationToken cancellationToken = default)
            => GetIdByCodeAsync(SystemCategoryCodes.GetBalanceAdjustmentCode(isIncome), cancellationToken);

        public async Task<(int fromCategoryId, int toCategoryId, string transferTypeName)> GetTransferCategoriesAsync(
            AccountType fromType,
            AccountType toType,
            CancellationToken cancellationToken = default)
        {
            var (fromCode, toCode) = SystemCategoryCodes.GetTransferCodesByAccountType(fromType, toType);

            var fromId = await GetIdByCodeAsync(fromCode, cancellationToken);
            var toId = await GetIdByCodeAsync(toCode, cancellationToken);
            var transferTypeName = SystemCategoryCodes.GetTransferTypeName(fromCode, toCode);

            return (fromId, toId, transferTypeName);
        }

        private async Task<int> GetIdByCodeAsync(string systemCategoryCode, CancellationToken cancellationToken)
        {
            var categoryId = await _dbContext.Categories
                .AsNoTracking()
                .Where(c => c.IsSystem && c.SystemCategoryCode == systemCategoryCode && !c.IsDeleted)
                .Select(c => c.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (categoryId <= 0)
            {
                throw new InvalidOperationException($"System category not found for code '{systemCategoryCode}'.");
            }

            return categoryId;
        }
    }
}
