using Microsoft.EntityFrameworkCore;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Infrastructure.Persistence
{
    public class TenantBootstrapService : ITenantBootstrapService
    {
        private readonly MoneyTrackerDbContext _dbContext;

        public TenantBootstrapService(MoneyTrackerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SeedDefaultsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var existingCount = await _dbContext.Categories
                .IgnoreQueryFilters()
                .CountAsync(c => c.TenantId == tenantId, cancellationToken);

            if (existingCount > 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var categories = new List<Category>
            {
                Create(tenantId, SystemCategoryNames.INITIAL_BALANCE_NAME, CategoryTypeEnum.Income, true, CategoryIcon.AccountBalanceWallet.ToString(), "#4CAF50", now, SystemCategoryCodes.InitialBalanceIncome),
                Create(tenantId, SystemCategoryNames.INITIAL_BALANCE_NAME, CategoryTypeEnum.Expense, true, CategoryIcon.AccountBalanceWallet.ToString(), "#F44336", now, SystemCategoryCodes.InitialBalanceExpense),
                Create(tenantId, SystemCategoryNames.BALANCE_ADJUSTMENT_NAME, CategoryTypeEnum.Income, true, CategoryIcon.CurrencyExchange.ToString(), "#4CAF50", now, SystemCategoryCodes.BalanceAdjustmentIncome),
                Create(tenantId, SystemCategoryNames.BALANCE_ADJUSTMENT_NAME, CategoryTypeEnum.Expense, true, CategoryIcon.CurrencyExchange.ToString(), "#F44336", now, SystemCategoryCodes.BalanceAdjustmentExpense),
                Create(tenantId, SystemCategoryNames.TRANSFER_OUT_NAME, CategoryTypeEnum.Transfer, true, CategoryIcon.TrendingDown.ToString(), "#FF9800", now, SystemCategoryCodes.TransferOut),
                Create(tenantId, SystemCategoryNames.TRANSFER_IN_NAME, CategoryTypeEnum.Transfer, true, CategoryIcon.TrendingUp.ToString(), "#4CAF50", now, SystemCategoryCodes.TransferIn),
                Create(tenantId, SystemCategoryNames.CREDIT_PAYMENT_NAME, CategoryTypeEnum.Transfer, true, CategoryIcon.CreditCard.ToString(), "#2196F3", now, SystemCategoryCodes.CreditPayment),
                Create(tenantId, SystemCategoryNames.PAYMENT_RECEIVED_NAME, CategoryTypeEnum.Transfer, true, CategoryIcon.Payments.ToString(), "#4CAF50", now, SystemCategoryCodes.PaymentReceived),
                Create(tenantId, SystemCategoryNames.CREDIT_ADVANCE_NAME, CategoryTypeEnum.Transfer, true, CategoryIcon.CreditScore.ToString(), "#FF5722", now, SystemCategoryCodes.CreditAdvance),
                Create(tenantId, SystemCategoryNames.ADVANCE_RECEIVED_NAME, CategoryTypeEnum.Transfer, true, CategoryIcon.Receipt.ToString(), "#4CAF50", now, SystemCategoryCodes.AdvanceReceived),
                Create(tenantId, "Salary", CategoryTypeEnum.Income, false, CategoryIcon.AttachMoney.ToString(), "#4CAF50", now),
                Create(tenantId, "Freelance", CategoryTypeEnum.Income, false, CategoryIcon.WorkOutline.ToString(), "#66BB6A", now),
                Create(tenantId, "Investments", CategoryTypeEnum.Income, false, CategoryIcon.TrendingUp.ToString(), "#81C784", now),
                Create(tenantId, "Food", CategoryTypeEnum.Expense, false, CategoryIcon.Restaurant.ToString(), "#FF5722", now),
                Create(tenantId, "Transport", CategoryTypeEnum.Expense, false, CategoryIcon.Commute.ToString(), "#2196F3", now),
                Create(tenantId, "Rent/Mortgage", CategoryTypeEnum.Expense, false, CategoryIcon.Home.ToString(), "#3F51B5", now),
                Create(tenantId, "Utilities", CategoryTypeEnum.Expense, false, CategoryIcon.Bolt.ToString(), "#FFC107", now),
                Create(tenantId, "Health", CategoryTypeEnum.Expense, false, CategoryIcon.LocalHospital.ToString(), "#F44336", now),
                Create(tenantId, "Entertainment", CategoryTypeEnum.Expense, false, CategoryIcon.SportsEsports.ToString(), "#9C27B0", now),
                Create(tenantId, "Shopping", CategoryTypeEnum.Expense, false, CategoryIcon.ShoppingCart.ToString(), "#E91E63", now),
                Create(tenantId, "Education", CategoryTypeEnum.Expense, false, CategoryIcon.School.ToString(), "#00BCD4", now),
                Create(tenantId, "Travel", CategoryTypeEnum.Expense, false, CategoryIcon.FlightTakeoff.ToString(), "#FF9800", now)
            };

            _dbContext.Categories.AddRange(categories);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static Category Create(
            Guid tenantId,
            string name,
            CategoryTypeEnum type,
            bool isSystem,
            string icon,
            string color,
            DateTime nowUtc,
            string? systemCategoryCode = null)
        {
            return new Category
            {
                TenantId = tenantId,
                Name = name,
                Type = type,
                IsSystem = isSystem,
                SystemCategoryCode = systemCategoryCode,
                Icon = icon,
                Color = color,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };
        }
    }
}
