using Microsoft.EntityFrameworkCore;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Infrastructure.Persistence.Repositories.Seeds
{
    public static class CategorySeed
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                // ========================
                // CATEGORÍAS DEL SISTEMA (IsSystem = true) - IDs 1-10
                // ========================
                new Category
                {
                    Id = 1,
                    Name = SystemCategoryNames.INITIAL_BALANCE_NAME,
                    Icon = CategoryIcon.AccountBalanceWallet.ToString(),
                    Color = "#4CAF50",
                    Type = CategoryTypeEnum.Income,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 2,
                    Name = SystemCategoryNames.INITIAL_BALANCE_NAME,
                    Icon = CategoryIcon.AccountBalanceWallet.ToString(),
                    Color = "#F44336",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 3,
                    Name = SystemCategoryNames.BALANCE_ADJUSTMENT_NAME,
                    Icon = CategoryIcon.CurrencyExchange.ToString(),
                    Color = "#4CAF50",
                    Type = CategoryTypeEnum.Income,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 4,
                    Name = SystemCategoryNames.BALANCE_ADJUSTMENT_NAME,
                    Icon = CategoryIcon.CurrencyExchange.ToString(),
                    Color = "#F44336",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = true,
                },

                // Transfer Categories (5-10) - AHORA CON TYPE = Transfer
                new Category
                {
                    Id = 5,
                    Name = SystemCategoryNames.TRANSFER_OUT_NAME,
                    Icon = CategoryIcon.TrendingDown.ToString(),
                    Color = "#FF9800",
                    Type = CategoryTypeEnum.Transfer,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 6,
                    Name = SystemCategoryNames.TRANSFER_IN_NAME,
                    Icon = CategoryIcon.TrendingUp.ToString(),
                    Color = "#4CAF50",
                    Type = CategoryTypeEnum.Transfer, 
                    IsSystem = true,
                },
                new Category
                {
                    Id = 7,
                    Name = SystemCategoryNames.CREDIT_PAYMENT_NAME,
                    Icon = CategoryIcon.CreditCard.ToString(),
                    Color = "#2196F3",
                    Type = CategoryTypeEnum.Transfer, 
                    IsSystem = true,
                },
                new Category
                {
                    Id = 8,
                    Name = SystemCategoryNames.PAYMENT_RECEIVED_NAME,
                    Icon = CategoryIcon.Payments.ToString(),
                    Color = "#4CAF50",
                    Type = CategoryTypeEnum.Transfer,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 9,
                    Name = SystemCategoryNames.CREDIT_ADVANCE_NAME,
                    Icon = CategoryIcon.CreditScore.ToString(),
                    Color = "#FF5722",
                    Type = CategoryTypeEnum.Transfer,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 10,
                    Name = SystemCategoryNames.ADVANCE_RECEIVED_NAME,
                    Icon = CategoryIcon.Receipt.ToString(),
                    Color = "#4CAF50",
                    Type = CategoryTypeEnum.Transfer, 
                    IsSystem = true,
                },

                // ========================
                // CATEGORÍAS DE USUARIO (IsSystem = false) - IDs 11+
                // ========================

                new Category
                {
                    Id = 11,
                    Name = "Salary",
                    Icon = CategoryIcon.AttachMoney.ToString(),
                    Color = "#4CAF50",
                    Type = CategoryTypeEnum.Income,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 12,
                    Name = "Freelance",
                    Icon = CategoryIcon.WorkOutline.ToString(),
                    Color = "#66BB6A",
                    Type = CategoryTypeEnum.Income,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 13,
                    Name = "Investments",
                    Icon = CategoryIcon.TrendingUp.ToString(),
                    Color = "#81C784",
                    Type = CategoryTypeEnum.Income,
                    IsSystem = false,
                },

                // Expense Categories (14-22)
                new Category
                {
                    Id = 14,
                    Name = "Food",
                    Icon = CategoryIcon.Restaurant.ToString(),
                    Color = "#FF5722",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 15,
                    Name = "Transport",
                    Icon = CategoryIcon.Commute.ToString(),
                    Color = "#2196F3",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 16,
                    Name = "Rent/Mortgage",
                    Icon = CategoryIcon.Home.ToString(),
                    Color = "#3F51B5",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 17,
                    Name = "Utilities",
                    Icon = CategoryIcon.Bolt.ToString(),
                    Color = "#FFC107",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 18,
                    Name = "Health",
                    Icon = CategoryIcon.LocalHospital.ToString(),
                    Color = "#F44336",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 19,
                    Name = "Entertainment",
                    Icon = CategoryIcon.SportsEsports.ToString(),
                    Color = "#9C27B0",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 20,
                    Name = "Shopping",
                    Icon = CategoryIcon.ShoppingCart.ToString(),
                    Color = "#E91E63",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 21,
                    Name = "Education",
                    Icon = CategoryIcon.School.ToString(),
                    Color = "#00BCD4",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 22,
                    Name = "Travel",
                    Icon = CategoryIcon.FlightTakeoff.ToString(),
                    Color = "#FF9800",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                }
            );
        }
    }
}