using Microsoft.EntityFrameworkCore;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Infrastructure.Persistence.Repositories.Seeds
{
    public static class CategorySeed
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
            // Income
            new Category
            {
                Id = 1,
                Name = "Salary",
                Icon = "attach_money",
                Color = "#4CAF50",
                Type = CategoryType.Income
            },
            new Category { Id = 2, Name = "Freelance", Icon = "work_outline", Color = "#66BB6A", Type = CategoryType.Income },
            new Category { Id = 3, Name = "Investments", Icon = "trending_up", Color = "#81C784", Type = CategoryType.Income },

             new Category
             {
                 Id = 4,
                 Name = "Initial Balance",
                 Icon = "account_balance_wallet",
                 Color = "#4CAF50", // Verde para Income
                 Type = CategoryType.Income
             },

            new Category
            {
                Id = 5,
                Name = "Initial Balance",
                Icon = "account_balance_wallet",
                Color = "#F44336", // Rojo para Expense
                Type = CategoryType.Expense
            },

            new Category
            {
                Id = 6,
                Name = "Balance Adjustment",
                Icon = "tune",
                Color = "#4CAF50", // Verde para Income
                Type = CategoryType.Income
            },
            new Category
            {
                Id = 7,
                Name = "Balance Adjustment",
                Icon = "tune",
                Color = "#F44336", // Rojo para Expense
                Type = CategoryType.Expense
            },

            // Expense
            new Category
            {
                Id = 10,
                Name = "Food",
                Icon = "restaurant",
                Color = "#FF5722",
                Type = CategoryType.Expense
            },
            new Category { Id = 11, Name = "Transport", Icon = "commute", Color = "#2196F3", Type = CategoryType.Expense },
            new Category { Id = 12, Name = "Rent", Icon = "home", Color = "#3F51B5", Type = CategoryType.Expense },
            new Category { Id = 13, Name = "Utilities", Icon = "bolt", Color = "#FFC107", Type = CategoryType.Expense },
            new Category { Id = 14, Name = "Health", Icon = "local_hospital", Color = "#F44336", Type = CategoryType.Expense },
            new Category { Id = 20, Name = "Entertainment", Icon = "sports_esports", Color = "#9C27B0", Type = CategoryType.Expense },
            new Category { Id = 21, Name = "Shopping", Icon = "shopping_cart", Color = "#E91E63", Type = CategoryType.Expense },
            new Category { Id = 22, Name = "Education", Icon = "school", Color = "#00BCD4", Type = CategoryType.Expense },
            new Category
            {
                Id = 23,
                Name = "Travel",
                Icon = "flight_takeoff",
                Color = "#FF9800",
                Type = CategoryType.Expense
            }
        );
        }
    }
}