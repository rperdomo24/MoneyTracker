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
                // Income Categories (1-3)
                new Category
                {
                    Id = 1,
                    Name = "Salario",
                    Icon = CategoryIcon.AttachMoney.ToString(),
                    Color = "#4CAF50",
                    Type = CategoryTypeEnum.Income,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 2,
                    Name = "Freelance",
                    Icon = CategoryIcon.WorkOutline.ToString(),
                    Color = "#66BB6A",
                    Type = CategoryTypeEnum.Income,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 3,
                    Name = "Inversiones",
                    Icon = CategoryIcon.TrendingUp.ToString(),
                    Color = "#81C784",
                    Type = CategoryTypeEnum.Income,
                    IsSystem = false,
                },

                // Balance Management Categories (4-7)
                new Category
                {
                    Id = 4,
                    Name = "Balance Inicial",
                    Icon = CategoryIcon.AccountBalanceWallet.ToString(),
                    Color = "#4CAF50", // Verde para Income
                    Type = CategoryTypeEnum.Income,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 5,
                    Name = "Balance Inicial",
                    Icon = CategoryIcon.AccountBalanceWallet.ToString(),
                    Color = "#F44336", // Rojo para Expense
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 6,
                    Name = "Ajuste de Balance",
                    Icon = CategoryIcon.CurrencyExchange.ToString(),
                    Color = "#4CAF50", // Verde para Income
                    Type = CategoryTypeEnum.Income,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 7,
                    Name = "Ajuste de Balance",
                    Icon = CategoryIcon.CurrencyExchange.ToString(),
                    Color = "#F44336", // Rojo para Expense
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = true,
                },

                // Transfer Categories (8-9)
                new Category
                {
                    Id = 8,
                    Name = "Transferencia Enviada",
                    Icon = CategoryIcon.TrendingDown.ToString(),
                    Color = "#FF9800", // Naranja para transfers
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 9,
                    Name = "Transferencia Recibida",
                    Icon = CategoryIcon.TrendingUp.ToString(),
                    Color = "#FF9800", // Naranja para transfers
                    Type = CategoryTypeEnum.Income,
                    IsSystem = true,
                },

                // Credit Payment Categories (10-11)
                new Category
                {
                    Id = 10,
                    Name = "Pago de Tarjeta",
                    Icon = CategoryIcon.CreditCard.ToString(),
                    Color = "#4CAF50", // Verde (good for net worth)
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 11,
                    Name = "Pago Recibido",
                    Icon = CategoryIcon.Payments.ToString(),
                    Color = "#4CAF50", // Verde (good for net worth)
                    Type = CategoryTypeEnum.Income,
                    IsSystem = true,
                },

                // Credit Advance Categories (12-13)
                new Category
                {
                    Id = 12,
                    Name = "Adelanto de Efectivo",
                    Icon = CategoryIcon.CreditScore.ToString(),
                    Color = "#F44336", // Rojo (bad for net worth)
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = true,
                },
                new Category
                {
                    Id = 13,
                    Name = "Adelanto Recibido",
                    Icon = CategoryIcon.Receipt.ToString(),
                    Color = "#F44336", // Rojo (bad for net worth)
                    Type = CategoryTypeEnum.Income,
                    IsSystem = true,
                },

                new Category
                {
                    Id = 14,
                    Name = "Comida",
                    Icon = CategoryIcon.Restaurant.ToString(),
                    Color = "#FF5722",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 15,
                    Name = "Transporte",
                    Icon = CategoryIcon.Commute.ToString(),
                    Color = "#2196F3",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 16,
                    Name = "Alquiler/Hipoteca",
                    Icon = CategoryIcon.Home.ToString(),
                    Color = "#3F51B5",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 17,
                    Name = "Servicios Públicos",
                    Icon = CategoryIcon.Bolt.ToString(),
                    Color = "#FFC107",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 18,
                    Name = "Salud",
                    Icon = CategoryIcon.LocalHospital.ToString(),
                    Color = "#F44336",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 19,
                    Name = "Entretenimiento",
                    Icon = CategoryIcon.SportsEsports.ToString(),
                    Color = "#9C27B0",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 20,
                    Name = "Compras",
                    Icon = CategoryIcon.ShoppingCart.ToString(),
                    Color = "#E91E63",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 21,
                    Name = "Educación",
                    Icon = CategoryIcon.School.ToString(),
                    Color = "#00BCD4",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
                },
                new Category
                {
                    Id = 22,
                    Name = "Viajes",
                    Icon = CategoryIcon.FlightTakeoff.ToString(),
                    Color = "#FF9800",
                    Type = CategoryTypeEnum.Expense,
                    IsSystem = false,
               
                }
            );
        }
    }
}