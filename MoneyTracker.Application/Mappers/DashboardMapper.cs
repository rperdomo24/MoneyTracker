using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Dashboard;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.Mappers
{
    public static class DashboardMapper
    {
        public static CashFlowCategoryDto MapToCashFlowCategory(this DashboardCategoryAggregateEntry entry)
            => new()
            {
                CategoryId = entry.CategoryId,
                CategoryName = entry.CategoryName,
                Amount = entry.Amount
            };

        public static CashFlowCategoryDto MapToCashFlowCategory(int categoryId, string categoryName, decimal amount)
            => new()
            {
                CategoryId = categoryId,
                CategoryName = categoryName,
                Amount = amount
            };

        public static CategoryBreakdownDto MapToCategoryBreakdown(this DashboardCategoryAggregateEntry entry, decimal totalExpenses)
            => new()
            {
                CategoryId = entry.CategoryId,
                CategoryName = entry.CategoryName,
                CategoryColor = entry.CategoryColor,
                Amount = entry.Amount,
                Percentage = totalExpenses > 0 ? (entry.Amount / totalExpenses) * 100 : 0,
                TransactionCount = entry.TransactionCount,
                AverageTransaction = entry.TransactionCount > 0 ? entry.Amount / entry.TransactionCount : 0
            };

        public static CategoryBreakdownDto MapToCategoryBreakdown(
            int categoryId,
            string categoryName,
            string categoryColor,
            decimal amount,
            decimal totalExpenses,
            int transactionCount)
            => new()
            {
                CategoryId = categoryId,
                CategoryName = categoryName,
                CategoryColor = categoryColor,
                Amount = amount,
                Percentage = totalExpenses > 0 ? (amount / totalExpenses) * 100 : 0,
                TransactionCount = transactionCount,
                AverageTransaction = transactionCount > 0 ? amount / transactionCount : 0
            };

        public static RecentTransactionDto MapToRecentTransaction(
            this TransactionDto transaction,
            DateTime now,
            Func<DateTime, DateTime, string> relativeFormatter)
            => new()
            {
                Id = transaction.Id,
                Name = transaction.Name,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Date = transaction.Date,
                AccountName = transaction.Account?.Name ?? DashboardConstants.UnknownAccountName,
                CategoryName = transaction.Category?.Name ?? DashboardConstants.UncategorizedName,
                CategoryColor = transaction.Category?.Color ?? DashboardConstants.DefaultCategoryColor,
                Type = transaction.TransactionType,
                RelativeTime = relativeFormatter(transaction.Date, now)
            };

        public static RecentTransactionDto MapToRecentTransaction(
            this DashboardTransactionEntry entry,
            DateTime localDate,
            TransactionTypeEnum type,
            string relativeTime)
            => new()
            {
                Id = entry.Id,
                Name = entry.Name,
                Description = entry.Description,
                Amount = entry.Amount,
                Date = localDate,
                AccountName = entry.AccountName,
                CategoryName = entry.CategoryName,
                CategoryColor = entry.CategoryColor,
                Type = type,
                RelativeTime = relativeTime
            };
    }
}
