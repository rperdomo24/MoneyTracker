using FluentAssertions;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Budgets;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Budgets;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class BudgetServiceTests
    {
        private readonly Mock<IBudgetRepository> _budgetRepo = new();
        private readonly Mock<ICategoryRepository> _categoryRepo = new();
        private readonly Mock<ITransactionRepository> _txRepo = new();
        private readonly Mock<ITimeZoneService> _tz = new();
        private readonly Mock<ILogger<BudgetService>> _logger = new();

        private BudgetService CreateService()
            => new BudgetService(_budgetRepo.Object,
                                 _categoryRepo.Object,
                                 _txRepo.Object,
                                 _tz.Object,
                                 _logger.Object);

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsFailResult()
        {
            // Arrange: repository returns null for the requested budget.
            _budgetRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                       .ReturnsAsync((Budget?)null);

            int budgetId = 999;
            var svc = CreateService();

            var result = await svc.GetByIdAsync(budgetId);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
            _budgetRepo.Verify(x => x.GetByIdAsync(budgetId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_ReturnsMappedBudget()
        {
            _budgetRepo.Setup(x => x.GetByIdAsync(7))
                .ReturnsAsync(new Budget
                {
                    Id = 7,
                    CategoryId = 2,
                    Year = 2026,
                    Month = 3,
                    Amount = 350m,
                    IncludeChildren = true
                });

            var svc = CreateService();

            var result = await svc.GetByIdAsync(7);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.DataRetrieved);
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(7);
            result.Data.Amount.Should().Be(350m);
        }

        [Fact]
        public async Task CreateOrUpdateAsync_WhenCategoryIdIsInvalid_ReturnsValidationMessage()
        {
            var svc = CreateService();

            var result = await svc.CreateOrUpdateAsync(new BudgetDto
            {
                CategoryId = 0,
                Year = 2026,
                Month = 3,
                Amount = 100m
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Be(ValidationMessages.Required);
            _budgetRepo.Verify(x => x.GetByCategoryMonthAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateOrUpdateAsync_WhenMonthIsInvalid_ReturnsFailResult()
        {
            var svc = CreateService();

            var result = await svc.CreateOrUpdateAsync(new BudgetDto
            {
                CategoryId = 1,
                Year = 2026,
                Month = 13,
                Amount = 100m
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid year/month.");
        }

        [Fact]
        public async Task CreateOrUpdateAsync_WhenAmountIsNegative_ReturnsFailResult()
        {
            var svc = CreateService();

            var result = await svc.CreateOrUpdateAsync(new BudgetDto
            {
                CategoryId = 1,
                Year = 2026,
                Month = 3,
                Amount = -1m
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Amount must be >= 0.");
        }

        [Fact]
        public async Task CreateOrUpdateAsync_NewBudget_CallsAddAndReturnsCreated()
        {
            // Arrange: no existing budget for (category, year, month).
            _budgetRepo.Setup(x => x.GetByCategoryMonthAsync(1, 2025, 4))
                .ReturnsAsync((Budget?)null);

            var dto = new BudgetDto
            {
                CategoryId = 1,
                Year = 2025,
                Month = 4,
                Amount = 100m,
                IncludeChildren = true,
                RolloverEnabled = false,
                RolloverMode = RolloverMode.None
            };

            var nowUtc = new DateTime(2026, 3, 5, 18, 0, 0, DateTimeKind.Utc);
            _tz.Setup(t => t.GetNowInUtc()).Returns(nowUtc);

            var svc = CreateService();

            var result = await svc.CreateOrUpdateAsync(dto);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Created);
            _budgetRepo.Verify(x => x.AddAsync(It.Is<Budget>(b =>
                b.CategoryId == dto.CategoryId &&
                b.Year == dto.Year &&
                b.Month == dto.Month &&
                b.Amount == dto.Amount &&
                b.CreatedAt == nowUtc &&
                b.UpdatedAt == nowUtc &&
                b.IsDeleted == false)),
                Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateAsync_ExistingBudget_CallsUpdateAndReturnsUpdated()
        {
            var existing = new Budget
            {
                Id = 10,
                CategoryId = 1,
                Year = 2026,
                Month = 3,
                Amount = 100m,
                IncludeChildren = false,
                RolloverEnabled = false,
                RolloverMode = RolloverMode.None
            };
            _budgetRepo.Setup(x => x.GetByCategoryMonthAsync(1, 2026, 3)).ReturnsAsync(existing);

            var nowUtc = new DateTime(2026, 3, 5, 19, 0, 0, DateTimeKind.Utc);
            _tz.Setup(t => t.GetNowInUtc()).Returns(nowUtc);
            var svc = CreateService();

            var result = await svc.CreateOrUpdateAsync(new BudgetDto
            {
                CategoryId = 1,
                Year = 2026,
                Month = 3,
                Amount = 250m,
                IncludeChildren = true,
                RolloverEnabled = true,
                RolloverMode = RolloverMode.CarryOverRemaining
            });

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Updated);
            existing.Amount.Should().Be(250m);
            existing.IncludeChildren.Should().BeTrue();
            existing.RolloverEnabled.Should().BeTrue();
            existing.RolloverMode.Should().Be(RolloverMode.CarryOverRemaining);
            existing.UpdatedAt.Should().Be(nowUtc);
            _budgetRepo.Verify(x => x.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenSuccess_CallsSoftDelete()
        {
            var svc = CreateService();

            var result = await svc.DeleteAsync(5);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Deleted);
            _budgetRepo.Verify(x => x.SoftDeleteAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetMonthlyWithUsageAsync_IncludesChildrenUsage()
        {
            // Arrange: one parent and one child expense category.
            var parent = new Domain.Entities.Category { Id = 1, Name = "Food", Type = CategoryTypeEnum.Expense };
            var child = new Domain.Entities.Category { Id = 2, Name = "Groceries", ParentId = 1, Type = CategoryTypeEnum.Expense };

            _categoryRepo.Setup(c => c.GetAllAsync(true, true))
                .ReturnsAsync(new List<Domain.Entities.Category> { parent, child });

            // Budget is defined on parent and includes descendants.
            _budgetRepo.Setup(b => b.GetByMonthAsync(2025, 4, null))
                .ReturnsAsync(new List<Budget>
                {
                    new Budget
                    {
                        Id = 5,
                        CategoryId = 1,
                        Year = 2025,
                        Month = 4,
                        Amount = 500,
                        IncludeChildren = true
                    }
                });

            // Transaction is charged to child category.
            _txRepo.Setup(t => t.GetFilteredAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                                                   It.IsAny<List<int>>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Domain.Entities.Transaction>
                {
                    new Domain.Entities.Transaction
                    {
                        CategoryId = 2,
                        Amount = -200m,
                        IsDeleted = false
                    }
                });

            // Keep UTC conversion deterministic for test readability.
            _tz.Setup(z => z.ConvertToUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);

            var svc = CreateService();

            var result = await svc.GetMonthlyWithUsageAsync(2025, 4);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            var parentBudget = result.Data.Find(x => x.Category.Id == 1);
            parentBudget.Should().NotBeNull();
            parentBudget!.Used.Should().Be(200m, "child usage must roll up to the parent budget");
        }

        [Fact]
        public async Task GetMonthlyWithUsageAsync_CreatesDefaultBudgetForCategoryWithoutBudget()
        {
            var category = new Domain.Entities.Category { Id = 4, Name = "Health", Type = CategoryTypeEnum.Expense };
            _categoryRepo.Setup(c => c.GetAllAsync(true, true)).ReturnsAsync(new List<Domain.Entities.Category> { category });
            _budgetRepo.Setup(b => b.GetByMonthAsync(2026, 3, null)).ReturnsAsync(new List<Budget>());
            _txRepo.Setup(t => t.GetFilteredAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Domain.Entities.Transaction>());
            _tz.Setup(z => z.ConvertToUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);

            var svc = CreateService();

            var result = await svc.GetMonthlyWithUsageAsync(2026, 3);

            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle();
            result.Data![0].Budget.Amount.Should().Be(0m);
            result.Data[0].Budget.CategoryId.Should().Be(4);
        }

        [Fact]
        public async Task GetMonthlyWithUsageAsync_IgnoresDeletedTransactionsAndTransferCategories()
        {
            var transfer = new Domain.Entities.Category { Id = 30, Name = "Transfer", Type = CategoryTypeEnum.Transfer };
            var expense = new Domain.Entities.Category { Id = 40, Name = "Bills", Type = CategoryTypeEnum.Expense };

            _categoryRepo.Setup(c => c.GetAllAsync(true, true)).ReturnsAsync(new List<Domain.Entities.Category> { transfer, expense });
            _budgetRepo.Setup(b => b.GetByMonthAsync(2026, 3, null)).ReturnsAsync(new List<Budget>());
            _txRepo.Setup(t => t.GetFilteredAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Domain.Entities.Transaction>
                {
                    new() { CategoryId = 30, Amount = -999m, IsDeleted = false }, // ignored (transfer category filtered out)
                    new() { CategoryId = 40, Amount = -50m, IsDeleted = true },   // ignored (soft deleted transaction)
                    new() { CategoryId = 40, Amount = -20m, IsDeleted = false }   // counted
                });
            _tz.Setup(z => z.ConvertToUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);

            var svc = CreateService();

            var result = await svc.GetMonthlyWithUsageAsync(2026, 3);

            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle(x => x.Category.Id == 40);
            result.Data![0].Used.Should().Be(20m);
        }

        [Fact]
        public async Task GetMonthlyWithUsageAsync_IncomeCategory_UsesSignedAmount()
        {
            var incomeCategory = new Domain.Entities.Category { Id = 50, Name = "Salary", Type = CategoryTypeEnum.Income };
            _categoryRepo.Setup(c => c.GetAllAsync(true, true)).ReturnsAsync(new List<Domain.Entities.Category> { incomeCategory });
            _budgetRepo.Setup(b => b.GetByMonthAsync(2026, 3, null)).ReturnsAsync(new List<Budget>());
            _txRepo.Setup(t => t.GetFilteredAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Domain.Entities.Transaction> { new() { CategoryId = 50, Amount = 1200m, IsDeleted = false } });
            _tz.Setup(z => z.ConvertToUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);

            var svc = CreateService();

            var result = await svc.GetMonthlyWithUsageAsync(2026, 3);

            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle();
            result.Data![0].Used.Should().Be(1200m);
        }

        [Fact]
        public async Task CreateOrUpdateAsync_WhenRepositoryThrows_ReturnsUnexpectedError()
        {
            _budgetRepo.Setup(x => x.GetByCategoryMonthAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("db down"));
            var svc = CreateService();

            var result = await svc.CreateOrUpdateAsync(new BudgetDto
            {
                CategoryId = 1,
                Year = 2026,
                Month = 3,
                Amount = 10m
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.UnexpectedError);
        }

        [Fact]
        public async Task DeleteAsync_WhenRepositoryThrows_ReturnsUnexpectedError()
        {
            _budgetRepo.Setup(x => x.SoftDeleteAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db down"));
            var svc = CreateService();

            var result = await svc.DeleteAsync(2);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.UnexpectedError);
        }

        [Fact]
        public async Task GetMonthlyWithUsageAsync_WhenRepositoryThrows_ReturnsUnexpectedError()
        {
            _budgetRepo.Setup(x => x.GetByMonthAsync(It.IsAny<int>(), It.IsAny<int>(), null))
                .ThrowsAsync(new Exception("db down"));
            var svc = CreateService();

            var result = await svc.GetMonthlyWithUsageAsync(2026, 3);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.UnexpectedError);
        }
    }
}
