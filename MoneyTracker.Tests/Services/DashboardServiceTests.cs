using FluentAssertions;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Dashboard;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Filters;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class DashboardServiceTests
    {
        private readonly Mock<IAccountService> _accountService = new();
        private readonly Mock<ITransactionService> _transactionService = new();
        private readonly Mock<ITimeZoneService> _timeZoneService = new();

        private DashboardService CreateService()
            => new(_accountService.Object, _transactionService.Object, _timeZoneService.Object);

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
            _transactionService.Setup(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Ok(new TransactionSummaryDto
                {
                    Transactions = new List<TransactionDto>
                    {
                        new() { Amount = 1000m, CategoryId = 1, Category = new CategoryDto { Type = CategoryTypeEnum.Income } },
                        new() { Amount = 300m, CategoryId = 2, Category = new CategoryDto { Type = CategoryTypeEnum.Expense } }
                    }
                }));
            var svc = CreateService();

            var result = await svc.GetCashFlowAsync(TimePeriodFilter.ThisMonth);

            result.Success.Should().BeTrue();
            result.Data!.TotalIncome.Should().Be(1000m);
            result.Data.TotalExpenses.Should().Be(300m);
            result.Data.NetCashFlow.Should().Be(700m);
            result.Data.DaysElapsed.Should().Be(10);
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
        public async Task GetBalanceTrendAsync_WhenSuccess_ReturnsRequestedMonths()
        {
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>())).Returns(new DateTime(2026, 3, 5));
            _accountService.Setup(x => x.GetAccountsWithBalancesAsync()).ReturnsAsync(OperationResult<List<AccountDto>>.Ok(new List<AccountDto>
            {
                new() { Type = AccountType.Checking, CurrentBalance = 500m },
                new() { Type = AccountType.Savings, CurrentBalance = 300m },
                new() { Type = AccountType.Credit, CurrentBalance = -100m }
            }));
            var svc = CreateService();

            var result = await svc.GetBalanceTrendAsync(4);

            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(4);
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

            _transactionService
                .Setup(x => x.GetFilteredAsync(It.Is<TransactionFilterDto>(f => f.TimePeriod == TimePeriodFilter.LastMonth)))
                .ReturnsAsync(OperationResult<TransactionSummaryDto>.Ok(new TransactionSummaryDto
                {
                    Transactions = new List<TransactionDto>
                    {
                        new()
                        {
                            Id = 3,
                            AccountId = 1,
                            Amount = 800m,
                            Date = new DateTime(2026, 2, 10),
                            Description = "Salary prev",
                            CategoryId = 1,
                            Category = new CategoryDto { Id = 1, Name = "Income", Type = CategoryTypeEnum.Income, Color = "#4caf50" },
                            Account = new AccountDto { Id = 1, Name = "Main" }
                        }
                    }
                }));

            var svc = CreateService();

            var result = await svc.GetOverviewAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Summary.NetWorth.Should().Be(750m);
            result.Data.CashFlow.NetCashFlow.Should().Be(800m);
            result.Data.RecentTransactions.Should().NotBeEmpty();

            _accountService.Verify(x => x.GetAccountsWithBalancesAsync(), Times.Once);
            _transactionService.Verify(x => x.GetFilteredAsync(It.IsAny<TransactionFilterDto>()), Times.Exactly(2));
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

            var result = await svc.GetOverviewAsync();

            result.Success.Should().BeFalse();
            result.Message.Should().Be("tx error");
        }
    }
}
