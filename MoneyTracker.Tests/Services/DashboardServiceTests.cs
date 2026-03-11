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
                .Setup(x => x.GetMonthlyWithUsageAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(OperationResult<List<BudgetWithUsageDto>>.Ok(new List<BudgetWithUsageDto>()));

            var svc = CreateService();
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
            // Credit balances are liabilities; non-credit positive balances are assets.
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
        public async Task GetCashFlowAsync_WhenSuccess_CalculatesTotalsAndBurnRate()
        {
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
            var svc = CreateService();

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
            var svc = CreateService();

            var result = await svc.GetBalanceTrendAsync(4);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            result.Data!.First().Date.Should().Be(new DateTime(2025, 12, 1));
            result.Data.Last().Date.Should().Be(new DateTime(2026, 3, 5));
        }

        [Fact]
        public async Task GetOverviewAsync_WhenSuccess_ReturnsAggregatedDataAndMinimizesCalls()
        {
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>())).Returns(new DateTime(2026, 3, 10, 12, 0, 0));

            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
                {
                    new() { Id = 1, Name = "Main", Type = AccountType.Bank, CurrentBalance = 1000m },
                    new() { Id = 2, Name = "Credit", Type = AccountType.Credit, CurrentBalance = -250m, CreditLimit = 1000m }
                }));

            _transactionService
                .Setup(x => x.GetFilteredAsync(It.Is<TransactionFilterDto>(f => f.TimePeriod == TimePeriodFilter.ThisMonth)))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Ok(new TransactionSummaryDto
                {
                    Transactions = new List<TransactionDto>
                    {
                        new()
                        {
                            Id = 1,
                            AccountId = 1,
                            Amount = 1000m,
                            Date = new DateTime(2026, 3, 2),
                            Description = "Salary",
                            CategoryId = 1,
                            Category = new CategoryDto { Id = 1, Name = "Income", Type = CategoryTypeEnum.Income, Color = "#4caf50" },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        },
                        new()
                        {
                            Id = 2,
                            AccountId = 1,
                            Amount = 200m,
                            Date = new DateTime(2026, 3, 4),
                            Description = "Groceries",
                            CategoryId = 2,
                            Category = new CategoryDto { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense, Color = "#f44336" },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        }
                    }
                }));

            var svc = CreateService();

            var result = await svc.GetOverviewAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.ThisMonth
            });

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Summary.NetWorth.Should().Be(750m);
            result.Data.CashFlow.NetCashFlow.Should().Be(800m);
            result.Data.RecentTransactions.Should().NotBeEmpty();

            _accountService.Verify(x => x.GetAccountsWithBalancesAsync(), Times.Once);
            _transactionService.Verify(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()), Times.Once);
        }

        [Fact]
        public async Task GetOverviewAsync_WhenThisMonthTransactionsFail_ReturnsFailResult()
        {
            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>()));

            _transactionService
                .Setup(x => x.GetFilteredAsync(It.Is<TransactionFilterDto>(f => f.TimePeriod == TimePeriodFilter.ThisMonth)))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Fail("tx error"));

            var svc = CreateService();

            var result = await svc.GetOverviewAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.ThisMonth
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Be("tx error");
        }

        [Fact]
        public async Task GetOverviewAsync_WhenFiltersProvided_AppliesAccountAndTransactionFilters()
        {
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>())).Returns(new DateTime(2026, 3, 10, 12, 0, 0));

            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
                {
                    new() { Id = 1, Name = "Main", Type = AccountType.Bank, CurrentBalance = 1000m },
                    new() { Id = 2, Name = "Savings", Type = AccountType.Savings, CurrentBalance = 500m }
                }));

            _transactionService
                .Setup(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Ok(new TransactionSummaryDto
                {
                    Transactions = new List<TransactionDto>
                    {
                        new()
                        {
                            Id = 1,
                            AccountId = 1,
                            Amount = 1000m,
                            Date = new DateTime(2026, 3, 2),
                            Description = "Salary",
                            CategoryId = 1,
                            Category = new CategoryDto { Id = 1, Name = "Income", Type = CategoryTypeEnum.Income, Color = "#4caf50" },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        },
                        new()
                        {
                            Id = 2,
                            AccountId = 1,
                            Amount = 300m,
                            Date = new DateTime(2026, 3, 3),
                            Description = "Food",
                            CategoryId = 2,
                            Category = new CategoryDto { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense, Color = "#f44336" },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        }
                    }
                }));

            var svc = CreateService();
            var filter = new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.Last30Days,
                AccountIds = new List<int> { 1 },
                TransactionFilter = DashboardTransactionFilter.Expense
            };

            var result = await svc.GetOverviewAsync(filter);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Summary.NetWorth.Should().Be(1000m);
            result.Data.CashFlow.TotalIncome.Should().Be(0m);
            result.Data.CashFlow.TotalExpenses.Should().Be(300m);

            _transactionService.Verify(x => x.GetFilteredAsync(
                It.Is<TransactionFilterDto>(f =>
                    f.TimePeriod == TimePeriodFilter.Last30Days &&
                    f.AccountIds.SequenceEqual(new[] { 1 }))),
                Times.Once);
        }

        [Fact]
        public async Task GetOverviewAsync_WhenTransactionsIncludeTransfer_ExcludesTransferFromGlobalStats()
        {
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns(new DateTime(2026, 3, 10, 12, 0, 0));

            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
                {
                    new() { Id = 1, Name = "Main", Type = AccountType.Bank, CurrentBalance = 1000m }
                }));

            _transactionService
                .Setup(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Ok(new TransactionSummaryDto
                {
                    Transactions = new List<TransactionDto>
                    {
                        new()
                        {
                            Id = 1,
                            AccountId = 1,
                            Amount = 1000m,
                            Date = new DateTime(2026, 3, 2),
                            Description = "Salary",
                            CategoryId = 1,
                            Category = new CategoryDto { Id = 1, Name = "Income", Type = CategoryTypeEnum.Income, Color = "#4caf50" },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        },
                        new()
                        {
                            Id = 2,
                            AccountId = 1,
                            Amount = 200m,
                            Date = new DateTime(2026, 3, 3),
                            Description = "Food",
                            CategoryId = 2,
                            Category = new CategoryDto { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense, Color = "#f44336" },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        },
                        new()
                        {
                            Id = 3,
                            AccountId = 1,
                            Amount = -200m,
                            Date = new DateTime(2026, 3, 4),
                            Description = "Transfer out",
                            CategoryId = 3,
                            Category = new CategoryDto { Id = 3, Name = "Transfer", Type = CategoryTypeEnum.Transfer, Color = "#2196f3", SystemCategoryCode = "TRANSFER_OUT" },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        }
                    }
                }));

            var svc = CreateService();

            var result = await svc.GetOverviewAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.Last30Days
            });

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.CashFlow.TotalIncome.Should().Be(1000m);
            result.Data.CashFlow.TotalExpenses.Should().Be(200m);
            result.Data.CashFlow.NetCashFlow.Should().Be(800m);
            result.Data.CashFlow.IncomeByCategory.Should().ContainSingle(x => x.CategoryName == "Income" && x.Amount == 1000m);
            result.Data.CashFlow.ExpenseByCategory.Should().ContainSingle(x => x.CategoryName == "Food" && x.Amount == 200m);
            result.Data.CategoryBreakdown.Should().ContainSingle();
            result.Data.SpendingTrend.Sum(x => x.Amount).Should().Be(200m);
            result.Data.RecentTransactions.Should().HaveCount(3);
            result.Data.RecentTransactions.Count(t => t.Type == Domain.Enums.Transaction.TransactionTypeEnum.Transfer).Should().Be(1);
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
        public async Task GetOverviewAsync_WhenBudgetDataExists_ReturnsBudgetMonthlySummary()
        {
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns(new DateTime(2026, 3, 10, 12, 0, 0));

            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
                {
                    new() { Id = 1, Name = "Main", Type = AccountType.Bank, CurrentBalance = 1200m }
                }));

            _transactionService
                .Setup(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Ok(new TransactionSummaryDto
                {
                    Transactions = new List<TransactionDto>()
                }));

            _budgetService
                .Setup(x => x.GetMonthlyWithUsageAsync(2026, 3))
                .ReturnsAsync(OperationResult<List<BudgetWithUsageDto>>.Ok(new List<BudgetWithUsageDto>
                {
                    new()
                    {
                        Budget = new BudgetDto { Amount = 500m },
                        Category = new CategoryDto { Id = 1, Name = "Food", Type = CategoryTypeEnum.Expense, Color = "#f44336" },
                        Used = 300m
                    },
                    new()
                    {
                        Budget = new BudgetDto { Amount = 200m },
                        Category = new CategoryDto { Id = 2, Name = "Transport", Type = CategoryTypeEnum.Expense, Color = "#ff9800" },
                        Used = 250m
                    }
                }));

            var svc = CreateService();
            var result = await svc.GetOverviewAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.Last30Days
            });

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.BudgetSummary.Year.Should().Be(2026);
            result.Data.BudgetSummary.Month.Should().Be(3);
            result.Data.BudgetSummary.TotalBudgeted.Should().Be(700m);
            result.Data.BudgetSummary.TotalUsed.Should().Be(550m);
            result.Data.BudgetSummary.TotalRemaining.Should().Be(150m);
            result.Data.BudgetSummary.OverBudgetCount.Should().Be(1);
            result.Data.BudgetSummary.NearLimitCount.Should().Be(1);
            result.Data.BudgetSummary.BudgetedCategoryCount.Should().Be(2);
            result.Data.BudgetSummary.HealthLabel.Should().Be("Over budget");
            result.Data.BudgetSummary.TopCategories.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetBudgetSummaryAsync_WhenParentHasNoBudgetButChildrenDo_GroupsChildrenIntoDashboardSummary()
        {
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns(new DateTime(2026, 3, 10, 12, 0, 0));

            _budgetService
                .Setup(x => x.GetMonthlyWithUsageAsync(2026, 3))
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

            var svc = CreateService();
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

        [Fact]
        public async Task GetOverviewAsync_WhenAllTimeSelected_CapsRangeToLast730Days()
        {
            var now = new DateTime(2026, 3, 10, 12, 0, 0);
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>())).Returns(now);

            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
                {
                    new() { Id = 1, Name = "Main", Type = AccountType.Bank, CurrentBalance = 1200m }
                }));

            _transactionService
                .Setup(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Ok(new TransactionSummaryDto
                {
                    Transactions = new List<TransactionDto>()
                }));

            _budgetService
                .Setup(x => x.GetMonthlyWithUsageAsync(now.Year, now.Month))
                .ReturnsAsync(OperationResult<List<BudgetWithUsageDto>>.Ok(new List<BudgetWithUsageDto>()));

            var svc = CreateService();
            var result = await svc.GetOverviewAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.AllTime
            });

            result.Success.Should().BeTrue();
            _transactionService.Verify(x => x.GetFilteredAsync(
                It.Is<TransactionFilterDto>(f =>
                    f.TimePeriod == TimePeriodFilter.Custom &&
                    f.FromDate == now.Date.AddDays(-730) &&
                    f.ToDate == now.Date)),
                Times.Once);
        }

        [Fact]
        public async Task GetOverviewAsync_WhenTransactionsContainOutOfRangeExpenses_SpendingTrendUsesOnlySelectedRange()
        {
            var now = new DateTime(2026, 3, 10, 12, 0, 0);
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>())).Returns(now);

            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
                {
                    new() { Id = 1, Name = "Main", Type = AccountType.Bank, CurrentBalance = 1000m }
                }));

            _transactionService
                .Setup(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Ok(new TransactionSummaryDto
                {
                    Transactions = new List<TransactionDto>
                    {
                        new()
                        {
                            Id = 1,
                            AccountId = 1,
                            Amount = 150m,
                            Date = new DateTime(2026, 2, 10),
                            CategoryId = 2,
                            Category = new CategoryDto { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        },
                        new()
                        {
                            Id = 2,
                            AccountId = 1,
                            Amount = 220m,
                            Date = new DateTime(2026, 3, 5),
                            CategoryId = 2,
                            Category = new CategoryDto { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        }
                    }
                }));

            _budgetService
                .Setup(x => x.GetMonthlyWithUsageAsync(now.Year, now.Month))
                .ReturnsAsync(OperationResult<List<BudgetWithUsageDto>>.Ok(new List<BudgetWithUsageDto>()));

            var svc = CreateService();
            var result = await svc.GetOverviewAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.ThisMonth
            });

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SpendingTrend.Sum(x => x.Amount).Should().Be(220m);
        }

        [Fact]
        public async Task GetOverviewAsync_User_fb847315_297e_4dee_be25_16b2393df7cb_WithLargeDataset_CompletesAndReturnsBoundedSeries()
        {
            // Regression test for reported case: dashboard appears to load forever for a user with very large history.
            var now = new DateTime(2026, 3, 10, 12, 0, 0);
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>())).Returns(now);

            _accountService.Setup(x => x.GetAccountsWithBalancesAsync())
                .ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
                {
                    new() { Id = 1, Name = "Main", Type = AccountType.Bank, CurrentBalance = 5000m }
                }));

            var start = now.Date.AddYears(-6);
            var transactions = Enumerable.Range(0, 6000)
                .Select(i => new TransactionDto
                {
                    Id = i + 1,
                    AccountId = 1,
                    Amount = (i % 3 == 0) ? 100m : 45m,
                    Date = start.AddDays(i % 2200),
                    CategoryId = (i % 3 == 0) ? 1 : 2,
                    Category = new CategoryDto
                    {
                        Id = (i % 3 == 0) ? 1 : 2,
                        Name = (i % 3 == 0) ? "Income" : "Expense",
                        Type = (i % 3 == 0) ? CategoryTypeEnum.Income : CategoryTypeEnum.Expense
                    },
                    Account = new AccountDto { Id = 1, Name = "Main" }
                })
                .ToList();

            _transactionService
                .Setup(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Ok(new TransactionSummaryDto
                {
                    Transactions = transactions
                }));

            _budgetService
                .Setup(x => x.GetMonthlyWithUsageAsync(now.Year, now.Month))
                .ReturnsAsync(OperationResult<List<BudgetWithUsageDto>>.Ok(new List<BudgetWithUsageDto>()));

            var svc = CreateService();

            var overviewTask = svc.GetOverviewAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.AllTime,
                AccountIds = new List<int> { 1 }
            });

            var completed = await Task.WhenAny(overviewTask, Task.Delay(TimeSpan.FromSeconds(5)));
            completed.Should().Be(overviewTask, "overview should complete and not stay in a perpetual loading state");

            var result = await overviewTask;
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Service now caps dashboard range; chart payload must remain bounded and UI-friendly.
            result.Data!.BalanceTrend.Count.Should().BeLessThanOrEqualTo(36);
            result.Data.SpendingTrend.Count.Should().BeLessThanOrEqualTo(36);
        }
    }
}
