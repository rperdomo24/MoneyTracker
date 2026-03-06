using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Filters;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _txRepo = new();
        private readonly Mock<IAccountRepository> _accountRepo = new();
        private readonly Mock<IValidator<TransactionDto>> _validator = new();
        private readonly Mock<IValidator<CreateTransferDto>> _transferValidator = new();
        private readonly Mock<ILogger<TransactionService>> _logger = new();
        private readonly Mock<ITimeZoneService> _tz = new();
        private readonly Mock<ITimeRangeService> _timeRange = new();
        private readonly Mock<ICategoryRepository> _categoryRepo = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<ISystemCategoryResolver> _systemCategoryResolver = new();

        private TransactionService CreateService()
            => new(
                _txRepo.Object,
                _accountRepo.Object,
                _validator.Object,
                _transferValidator.Object,
                _logger.Object,
                _tz.Object,
                _timeRange.Object,
                _categoryRepo.Object,
                _categoryService.Object,
                _systemCategoryResolver.Object);

        [Fact]
        public async Task CreateAsync_WhenValidationFails_ReturnsValidationMessage()
        {
            _validator.Setup(x => x.ValidateAsync(It.IsAny<TransactionDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Amount", "Amount is required") }));
            var svc = CreateService();

            var result = await svc.CreateAsync(new TransactionDto());

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Amount is required");
            _txRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenValid_AddsTransactionAndUpdatesAccountBalance()
        {
            // For expense categories, mapper converts amount to a negative delta on account balance.
            _validator.Setup(x => x.ValidateAsync(It.IsAny<TransactionDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _tz.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);
            _tz.Setup(x => x.GetNowInUtc()).Returns(new DateTime(2026, 3, 5, 18, 0, 0, DateTimeKind.Utc));

            _categoryService.Setup(x => x.GetByIdAsync(2))
                .ReturnsAsync(OperationResult<CategoryDto?>.Ok(new CategoryDto { Id = 2, Type = CategoryTypeEnum.Expense }));

            var account = new Account { Id = 10, Name = "Cash", Balance = 100m, Icon = "Wallet", Type = AccountType.Cash };
            _accountRepo.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(account);
            _accountRepo.Setup(x => x.UpdateAsync(It.IsAny<Account>())).ReturnsAsync(true);
            _txRepo.Setup(x => x.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            var svc = CreateService();
            var dto = new TransactionDto
            {
                Name = "Lunch",
                AccountId = 10,
                CategoryId = 2,
                Amount = 20m,
                Date = new DateTime(2026, 3, 5),
                CreatedAt = new DateTime(2026, 3, 5),
                UpdatedAt = new DateTime(2026, 3, 5),
                Category = new CategoryDto { Id = 2, Type = CategoryTypeEnum.Expense }
            };

            var result = await svc.CreateAsync(dto);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Created);
            _txRepo.Verify(x => x.AddAsync(It.Is<Transaction>(t => t.AccountId == 10 && t.Amount == -20m)), Times.Once);
            _accountRepo.Verify(x => x.UpdateAsync(It.Is<Account>(a => a.Id == 10 && a.Balance == 80m)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_ReturnsFailResult()
        {
            _txRepo.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Transaction?)null);
            var svc = CreateService();

            var result = await svc.DeleteAsync(99);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task DuplicateTransactionAsync_WhenCreditRelatedCategory_ReturnsFailResult()
        {
            // Business rule: credit-related transfer/payment categories cannot be duplicated.
            _txRepo.Setup(x => x.GetByIdAsync(4))
                .ReturnsAsync(new Transaction
                {
                    Id = 4,
                    CategoryId = 10,
                    Category = new Category
                    {
                        Id = 10,
                        Name = SystemCategoryNames.CREDIT_PAYMENT_NAME,
                        Type = CategoryTypeEnum.Transfer,
                        SystemCategoryCode = SystemCategoryCodes.CreditPayment,
                        Icon = "CreditCard"
                    }
                });
            var svc = CreateService();

            var result = await svc.DuplicateTransactionAsync(4);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.CreditPaymentCannotDuplicate);
        }

        [Fact]
        public async Task GetFilteredAsync_AppliesSearchAndCategoryFilters()
        {
            // Repository returns a superset; service must apply app-layer text/category/type filters.
            _timeRange.Setup(x => x.GetDateRangeUtc(TimePeriodFilter.ThisMonth, null, null)).Returns((null, null));
            _tz.Setup(x => x.ConvertFromUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);
            _txRepo.Setup(x => x.GetFilteredAsync(null, null, It.IsAny<List<int>>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Transaction>
                {
                    new()
                    {
                        Id = 1,
                        Name = "Market",
                        Description = "Groceries",
                        Amount = -50m,
                        CategoryId = 2,
                        Date = new DateTime(2026, 3, 2),
                        CreatedAt = new DateTime(2026, 3, 2),
                        Category = new Category { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense, Icon = "Restaurant" }
                    },
                    new()
                    {
                        Id = 2,
                        Name = "Salary",
                        Description = "Company",
                        Amount = 1000m,
                        CategoryId = 1,
                        Date = new DateTime(2026, 3, 1),
                        CreatedAt = new DateTime(2026, 3, 1),
                        Category = new Category { Id = 1, Name = "Income", Type = CategoryTypeEnum.Income, Icon = "AttachMoney" }
                    }
                });

            var svc = CreateService();
            var filter = new TransactionFilterDto
            {
                TimePeriod = TimePeriodFilter.ThisMonth,
                SearchText = "mark",
                CategoryId = 2,
                Type = CategoryTypeEnum.Expense
            };

            var result = await svc.GetFilteredAsync(filter);

            result.Success.Should().BeTrue();
            result.Data!.TotalCount.Should().Be(1);
            result.Data.Transactions[0].Name.Should().Be("Market");
        }

        [Fact]
        public async Task GetAccountBalanceAsync_WhenRepositoryThrows_ReturnsUnexpectedError()
        {
            _txRepo.Setup(x => x.GetAccountBalanceAsync(3)).ThrowsAsync(new Exception("db down"));
            var svc = CreateService();

            var result = await svc.GetAccountBalanceAsync(3);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.UnexpectedError);
        }
    }
}
