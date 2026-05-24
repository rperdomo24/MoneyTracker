using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.DTOs.Reports;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.CardBenefits;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class ReportServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactionRepo = new();
        private readonly Mock<ICardBenefitRepository> _cardBenefitRepo = new();
        private readonly Mock<ITimeZoneService> _timeZoneService = new();

        private static IConfiguration EmptyConfig =>
            new ConfigurationBuilder().Build();

        private ReportService CreateService()
            => new ReportService(
                _transactionRepo.Object,
                _cardBenefitRepo.Object,
                _timeZoneService.Object,
                NullLogger<ReportService>.Instance,
                Options.Create(new OpenAiSettings()),
                Options.Create(new GoogleAiSettings()),
                EmptyConfig);

        private void SetupTimeZone()
        {
            _timeZoneService.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);
            _timeZoneService.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>()))
                .Returns((DateTime d) => d);
        }

        private static Category ExpenseCategory(int id = 1, string name = "Food")
            => new() { Id = id, Name = name, Type = CategoryTypeEnum.Expense };

        private static Category IncomeCategory(int id = 10, string name = "Salary")
            => new() { Id = id, Name = name, Type = CategoryTypeEnum.Income };

        private static Account MakeAccount(int id = 1, string name = "Visa", string? bank = "BankX")
            => new() { Id = id, Name = name, BankName = bank };

        [Fact]
        public async Task GetMonthlyReportAsync_EmptyMonth_ReturnsSummaryWithZeros()
        {
            SetupTimeZone();
            _transactionRepo.Setup(x => x.GetFilteredAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Transaction>());
            _cardBenefitRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<CardBenefit>());

            var result = await CreateService().GetMonthlyReportAsync(new MonthlyReportFilterDto { Year = 2026, Month = 1 });

            result.Success.Should().BeTrue();
            result.Data!.Summary.TotalExpenses.Should().Be(0);
            result.Data.Summary.TotalIncome.Should().Be(0);
            result.Data.Transactions.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMonthlyReportAsync_WithTransactions_CalculatesSummaryCorrectly()
        {
            SetupTimeZone();
            var account = MakeAccount();
            var expenseCat = ExpenseCategory();
            var incomeCat = IncomeCategory();

            var transactions = new List<Transaction>
            {
                new() { Id = 1, Name = "Supermarket", Amount = -50m, Date = new DateTime(2026, 5, 3), Category = expenseCat, CategoryId = expenseCat.Id, Account = account, AccountId = account.Id, PaymentMethod = PaymentMethodEnum.CreditCard },
                new() { Id = 2, Name = "Restaurant", Amount = -30m, Date = new DateTime(2026, 5, 10), Category = expenseCat, CategoryId = expenseCat.Id, Account = account, AccountId = account.Id, PaymentMethod = PaymentMethodEnum.Cash },
                new() { Id = 3, Name = "Salary", Amount = 1000m, Date = new DateTime(2026, 5, 1), Category = incomeCat, CategoryId = incomeCat.Id, Account = account, AccountId = account.Id }
            };

            _transactionRepo.Setup(x => x.GetFilteredAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<bool>()))
                .ReturnsAsync(transactions);
            _cardBenefitRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<CardBenefit>());

            var result = await CreateService().GetMonthlyReportAsync(new MonthlyReportFilterDto { Year = 2026, Month = 5 });

            result.Success.Should().BeTrue();
            var summary = result.Data!.Summary;
            summary.TotalExpenses.Should().Be(80m);
            summary.TotalIncome.Should().Be(1000m);
            summary.FinalBalance.Should().Be(920m);
            summary.TotalCard.Should().Be(50m);
            summary.TotalCash.Should().Be(30m);
            summary.TransactionCount.Should().Be(3);
        }

        [Fact]
        public async Task GetMonthlyReportAsync_WithBenefitRules_CalculatesEstimatedBenefit()
        {
            SetupTimeZone();
            var account = MakeAccount(id: 1);
            var cat = ExpenseCategory(id: 5, name: "Groceries");

            var transactions = new List<Transaction>
            {
                new() { Id = 1, Name = "Supermarket", Amount = -100m, Date = new DateTime(2026, 5, 3), Category = cat, CategoryId = cat.Id, Account = account, AccountId = account.Id, PaymentMethod = PaymentMethodEnum.CreditCard }
            };

            var benefits = new List<CardBenefit>
            {
                new() { Id = 1, AccountId = 1, Account = account, Name = "5% Groceries", BenefitType = BenefitType.Cashback, BenefitRate = 0.05m, CategoryId = cat.Id, IsActive = true }
            };

            _transactionRepo.Setup(x => x.GetFilteredAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<bool>()))
                .ReturnsAsync(transactions);
            _cardBenefitRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(benefits);

            var result = await CreateService().GetMonthlyReportAsync(new MonthlyReportFilterDto { Year = 2026, Month = 5 });

            result.Success.Should().BeTrue();
            var cardSummary = result.Data!.CardSummary.First();
            cardSummary.EstimatedBenefit.Should().Be(5m);
        }

        [Fact]
        public async Task GetMonthlyReportAsync_WrongCardUsed_GeneratesRecommendation()
        {
            SetupTimeZone();
            var usedAccount = MakeAccount(id: 1, name: "Basic Card", bank: "BankA");
            var betterAccount = MakeAccount(id: 2, name: "Rewards Card", bank: "BankB");
            var cat = ExpenseCategory(id: 5, name: "Groceries");

            var transactions = new List<Transaction>
            {
                new() { Id = 1, Name = "Supermarket", Amount = -200m, Date = new DateTime(2026, 5, 3), Category = cat, CategoryId = cat.Id, Account = usedAccount, AccountId = usedAccount.Id, PaymentMethod = PaymentMethodEnum.CreditCard }
            };

            var benefits = new List<CardBenefit>
            {
                new() { Id = 1, AccountId = 1, Account = usedAccount, Name = "1% Basic", BenefitType = BenefitType.Cashback, BenefitRate = 0.01m, CategoryId = cat.Id, IsActive = true },
                new() { Id = 2, AccountId = 2, Account = betterAccount, Name = "5% Groceries", BenefitType = BenefitType.Cashback, BenefitRate = 0.05m, CategoryId = cat.Id, IsActive = true }
            };

            _transactionRepo.Setup(x => x.GetFilteredAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<bool>()))
                .ReturnsAsync(transactions);
            _cardBenefitRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(benefits);

            var result = await CreateService().GetMonthlyReportAsync(new MonthlyReportFilterDto { Year = 2026, Month = 5 });

            result.Success.Should().BeTrue();
            var recs = result.Data!.Recommendations;
            recs.Should().NotBeEmpty();
            recs.First().Type.Should().Be(RecommendationType.WrongCardUsed);
            recs.First().EstimatedLoss.Should().Be(8m); // (0.05 - 0.01) * 200 = 8
            recs.First().SuggestedCard.Should().Be("Rewards Card");
        }
    }
}
