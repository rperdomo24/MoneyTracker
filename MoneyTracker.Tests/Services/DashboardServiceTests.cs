using FluentAssertions;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Budgets;
using MoneyTracker.Application.DTOs.Dashboard;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Filters;
using MoneyTracker.Domain.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;

namespace MoneyTracker.Tests.Services
{
    public class DashboardServiceTests
    {
        private readonly Mock<IAccountService> _accountService = new();
        private readonly Mock<ITransactionService> _transactionService = new();
        private readonly Mock<ITransactionRepository> _transactionRepository = new();
        private readonly Mock<IBudgetService> _budgetService = new();
        private readonly Mock<ITimeZoneService> _timeZoneService = new();
        private readonly Mock<ILogger<DashboardService>> _logger = new();

        private DashboardService CreateService()
        {
            _timeZoneService.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);

            _transactionRepository
                .Setup(x => x.GetTrendEntriesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<CategoryTypeEnum?>()))
                .ReturnsAsync(new List<TransactionTrendEntry>());

            _transactionRepository
                .Setup(x => x.GetDashboardEntriesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>()))
                .ReturnsAsync(new List<DashboardTransactionEntry>());

            _transactionRepository
                .Setup(x => x.GetCategoryAggregatesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<CategoryTypeEnum>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new List<DashboardCategoryAggregateEntry>());

            _transactionRepository
                .Setup(x => x.GetAmountsByDateAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<CategoryTypeEnum>()))
                .ReturnsAsync(new List<DashboardAmountByDateEntry>());

            _transactionRepository
                .Setup(x => x.GetRecentDashboardEntriesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<CategoryTypeEnum?>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new List<DashboardTransactionEntry>());

            _transactionRepository
                .Setup(x => x.GetCashFlowAggregatesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>()))
                .ReturnsAsync(new List<DashboardCashFlowAggregateEntry>());

            return new DashboardService(
                _accountService.Object,
                _transactionService.Object,
                _transactionRepository.Object,
                _budgetService.Object,
                _timeZoneService.Object,
                _logger.Object);
        }

        [Fact]
        public async Task GetOverviewWidgetsAsync_WhenSuccess_UsesSingleDashboardQueryAndBuildsWidgets()
        {
            var svc = CreateService();

            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);
            _timeZoneService.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);

            _transactionRepository
                .Setup(x => x.GetDashboardEntriesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>()))
                .ReturnsAsync(new List<DashboardTransactionEntry>
                {
                    new()
                    {
                        Id = 1,
                        Name = "Salary",
                        Description = "Monthly salary",
                        Amount = 1500m,
                        Date = new DateTime(2026, 3, 2),
                        AccountId = 1,
                        AccountName = "Main",
                        CategoryId = 1,
                        CategoryName = "Income",
                        CategoryColor = "#4caf50",
                        CategoryType = CategoryTypeEnum.Income
                    },
                    new()
                    {
                        Id = 2,
                        Name = "Food",
                        Description = "Groceries",
                        Amount = 300m,
                        Date = new DateTime(2026, 3, 5),
                        AccountId = 1,
                        AccountName = "Main",
                        CategoryId = 2,
                        CategoryName = "Food",
                        CategoryColor = "#f44336",
                        CategoryType = CategoryTypeEnum.Expense
                    }
                });

            _budgetService
                .Setup(x => x.GetMonthlyWithUsageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(OperationResult<List<BudgetWithUsageDto>>.Ok(new List<BudgetWithUsageDto>()));

            var result = await svc.GetOverviewWidgetsAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.ThisMonth
            }, recentTransactionsCount: 6);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.CashFlow.TotalIncome.Should().Be(1500m);
            result.Data.CashFlow.TotalExpenses.Should().Be(300m);
            result.Data.CategoryBreakdown.Should().ContainSingle(x => x.CategoryName == "Food");
            result.Data.RecentTransactions.Should().HaveCount(2);

            _transactionRepository.Verify(x => x.GetDashboardEntriesAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<List<int>>()),
                Times.Once);

            _transactionService.Verify(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()), Times.Never);
        }

        [Fact]
        public async Task GetSpendingTrendAsync_WhenEntryFallsNearUtcMidnight_BucketsByLocalDate()
        {
            var svc = CreateService();

            _timeZoneService.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d.AddHours(-6));

            _transactionRepository
                .Setup(x => x.GetDashboardEntriesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>()))
                .ReturnsAsync(new List<DashboardTransactionEntry>
                {
                    new()
                    {
                        Id = 1,
                        Name = "Car insurance",
                        Description = string.Empty,
                        Amount = 300m,
                        Date = new DateTime(2026, 3, 2, 3, 30, 0), // Local UTC-6 => 2026-03-01 21:30
                        AccountId = 1,
                        AccountName = "Main",
                        CategoryId = 2,
                        CategoryName = "Transport",
                        CategoryColor = "#f44336",
                        CategoryType = CategoryTypeEnum.Expense
                    },
                    new()
                    {
                        Id = 2,
                        Name = "Food",
                        Description = string.Empty,
                        Amount = 100m,
                        Date = new DateTime(2026, 3, 2, 18, 0, 0), // Local UTC-6 => 2026-03-02 12:00
                        AccountId = 1,
                        AccountName = "Main",
                        CategoryId = 3,
                        CategoryName = "Food",
                        CategoryColor = "#ff9800",
                        CategoryType = CategoryTypeEnum.Expense
                    }
                });

            var result = await svc.GetSpendingTrendAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.Custom,
                FromDate = new DateTime(2026, 3, 1),
                ToDate = new DateTime(2026, 3, 2)
            });

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();

            result.Data!.Single(x => x.Date.Date == new DateTime(2026, 3, 1)).Amount.Should().Be(300m);
            result.Data.Single(x => x.Date.Date == new DateTime(2026, 3, 2)).Amount.Should().Be(100m);

            _transactionRepository.Verify(x => x.GetDashboardEntriesAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<List<int>>()),
                Times.Once);

            _transactionRepository.Verify(x => x.GetAmountsByDateAsync(
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<List<int>>(),
                It.IsAny<CategoryTypeEnum>()),
                Times.Never);
        }

        [Fact]
        public async Task GetSummaryAsync_WhenAccountsFail_ReturnsFailResult()
        {
            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Fail("accounts error"));
            var svc = CreateService();

            var result = await svc.GetSummaryAsync();

            result.Success.Should().BeFalse();
            result.Message.Should().Be("accounts error");
        }

        [Fact]
        public async Task GetSummaryAsync_WhenSuccess_CalculatesAssetsAndNetWorth()
        {
            // Assets include any positive account balance; liabilities are the absolute value of negative balances.
            _accountService.Setup(x => x.GetAccountsWithBalancesAsync()).ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
            {
                new() { Id = 1, Name = "Cash", Type = AccountType.Cash, CurrentBalance = 1000m },
                new() { Id = 2, Name = "Credit", Type = AccountType.Credit, CurrentBalance = -300m },
                new() { Id = 3, Name = "Bank", Type = AccountType.Bank, CurrentBalance = 200m }
            }));
            var svc = CreateService();

            var result = await svc.GetSummaryAsync();

            result.Success.Should().BeTrue();
            result.Data!.TotalAssets.Should().Be(1200m);
            result.Data.TotalLiabilities.Should().Be(300m);
            result.Data.NetWorth.Should().Be(900m);
        }

        [Fact]
        public async Task GetSummaryAsync_WhenCreditAccountHasPositiveBalance_IncludesItAsAsset()
        {
            _accountService.Setup(x => x.GetAccountsWithBalancesAsync()).ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
            {
                new() { Id = 1, Name = "Checking", Type = AccountType.Checking, CurrentBalance = 469.51m },
                new() { Id = 2, Name = "Credit", Type = AccountType.Credit, CurrentBalance = 40.44m }
            }));

            var svc = CreateService();
            var result = await svc.GetSummaryAsync();

            result.Success.Should().BeTrue();
            result.Data!.TotalAssets.Should().Be(509.95m);
            result.Data.TotalLiabilities.Should().Be(0m);
            result.Data.NetWorth.Should().Be(509.95m);
        }

        [Fact]
        public async Task GetCashFlowAsync_WhenSuccess_CalculatesTotalsAndBurnRate()
        {
            var svc = CreateService();

            // Use a fixed local date so monthly burn-rate fields are deterministic.
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>())).Returns(new DateTime(2026, 3, 10, 12, 0, 0));
            _timeZoneService.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);
            _transactionRepository
                .Setup(x => x.GetCashFlowAggregatesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>()))
                .ReturnsAsync(new List<DashboardCashFlowAggregateEntry>
                {
                    new() { CategoryType = CategoryTypeEnum.Income, CategoryId = 1, CategoryName = "Salary", Amount = 1000m, TransactionCount = 1 },
                    new() { CategoryType = CategoryTypeEnum.Expense, CategoryId = 2, CategoryName = "Food", Amount = 300m, TransactionCount = 1 }
                });

            var result = await svc.GetCashFlowAsync(TimePeriodFilter.ThisMonth);

            result.Success.Should().BeTrue();
            result.Data!.TotalIncome.Should().Be(1000m);
            result.Data.TotalExpenses.Should().Be(300m);
            result.Data.NetCashFlow.Should().Be(700m);
            result.Data.DaysElapsed.Should().Be(10);
            result.Data.IncomeByCategory.Should().ContainSingle();
            result.Data.ExpenseByCategory.Should().ContainSingle();
        }

        [Fact]
        public async Task GetAlertsAsync_WhenNoIssues_ReturnsHealthyAlert()
        {
            // If no warning thresholds are met, service should emit a single healthy status alert.
            _accountService.Setup(x => x.GetAccountsWithBalancesAsync()).ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
            {
                new() { Name = "Main", Type = AccountType.Bank, CurrentBalance = 1000m }
            }));
            var svc = CreateService();

            var result = await svc.GetAlertsAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle();
            result.Data![0].Type.Should().Be("Healthy");
        }

        [Fact]
        public async Task GetBalanceTrendAsync_WhenSuccess_ReturnsTrendRange()
        {
            var svc = CreateService();

            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>())).Returns(new DateTime(2026, 3, 5));
            _timeZoneService.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);
            _accountService.Setup(x => x.GetAccountsWithBalancesAsync()).ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
            {
                new() { Type = AccountType.Checking, CurrentBalance = 500m },
                new() { Type = AccountType.Savings, CurrentBalance = 300m },
                new() { Type = AccountType.Credit, CurrentBalance = -100m }
            }));
            _transactionRepository
                .Setup(x => x.GetTrendEntriesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<CategoryTypeEnum?>()))
                .ReturnsAsync(new List<TransactionTrendEntry>
                {
                    new() { Date = new DateTime(2026, 1, 15), Amount = 50m, CategoryType = CategoryTypeEnum.Income },
                    new() { Date = new DateTime(2026, 2, 10), Amount = 10m, CategoryType = CategoryTypeEnum.Expense }
                });

            var result = await svc.GetBalanceTrendAsync(4);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            result.Data!.First().Date.Should().Be(new DateTime(2025, 12, 1));
            result.Data.Last().Date.Should().Be(new DateTime(2026, 3, 5));
        }

        [Fact]
        public async Task GetBalanceTrendAsync_WhenCreditAccountHasPositiveBalance_EndPointMatchesSignedAccountSum()
        {
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns(new DateTime(2026, 3, 5, 12, 0, 0));
            _timeZoneService.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);

            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
                {
                    new() { Id = 1, Name = "Checking", Type = AccountType.Checking, CurrentBalance = 469.51m },
                    new() { Id = 2, Name = "Credit", Type = AccountType.Credit, CurrentBalance = 40.44m }
                }));

            _transactionRepository
                .Setup(x => x.GetTrendEntriesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<CategoryTypeEnum?>()))
                .ReturnsAsync(new List<TransactionTrendEntry>());

            var svc = CreateService();
            var result = await svc.GetBalanceTrendAsync(1);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            result.Data!.Last().TotalBalance.Should().Be(509.95m);
        }

        [Fact]
        public async Task GetBalanceTrendAsync_WhenTransactionsIncludeTransfer_ExcludesTransferFromNetWorthDelta()
        {
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns(new DateTime(2026, 3, 5, 12, 0, 0));
            _timeZoneService.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);

            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
                {
                    new() { Id = 1, Name = "Main", Type = AccountType.Bank, CurrentBalance = 1000m }
                }));

            _transactionRepository
                .Setup(x => x.GetTrendEntriesAsync(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<CategoryTypeEnum?>()))
                .ReturnsAsync(new List<TransactionTrendEntry>
                {
                    new() { Date = new DateTime(2026, 3, 2), Amount = 100m, CategoryType = CategoryTypeEnum.Income },
                    new() { Date = new DateTime(2026, 3, 3), Amount = 50m, CategoryType = CategoryTypeEnum.Expense }
                });

            var svc = CreateService();

            var result = await svc.GetBalanceTrendAsync(1);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            result.Data!.Last().TotalBalance.Should().Be(1000m);
        }

        [Fact]
        public async Task GetBudgetSummaryAsync_WhenParentHasNoBudgetButChildrenDo_GroupsChildrenIntoDashboardSummary()
        {
            var svc = CreateService();

            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns(new DateTime(2026, 3, 10, 12, 0, 0));

            _budgetService
                .Setup(x => x.GetMonthlyWithUsageAsync(2026, 3, It.IsAny<int>()))
                .ReturnsAsync(OperationResult<List<BudgetWithUsageDto>>.Ok(new List<BudgetWithUsageDto>
                {
                    new()
                    {
                        Budget = new BudgetDto { CategoryId = 10, Amount = 0m },
                        Category = new CategoryDto { Id = 10, Name = "Living", Type = CategoryTypeEnum.Expense, Color = "#123456" },
                        Used = 0m
                    },
                    new()
                    {
                        Budget = new BudgetDto { CategoryId = 11, Amount = 300m },
                        Category = new CategoryDto { Id = 11, ParentId = 10, Name = "Groceries", Type = CategoryTypeEnum.Expense, Color = "#4caf50" },
                        Used = 240m
                    },
                    new()
                    {
                        Budget = new BudgetDto { CategoryId = 12, Amount = 200m },
                        Category = new CategoryDto { Id = 12, ParentId = 10, Name = "Household", Type = CategoryTypeEnum.Expense, Color = "#ff9800" },
                        Used = 90m
                    }
                }));

            var result = await svc.GetBudgetSummaryAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.ThisMonth
            });

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalBudgeted.Should().Be(500m);
            result.Data.TotalUsed.Should().Be(330m);
            result.Data.BudgetedCategoryCount.Should().Be(1);
            result.Data.TopCategories.Should().ContainSingle();
            result.Data.TopCategories[0].CategoryName.Should().Be("Living");
            result.Data.TopCategories[0].IsGroupOnly.Should().BeTrue();
            result.Data.TopCategories[0].ProgressPercent.Should().Be(66);
        }

    }
}
