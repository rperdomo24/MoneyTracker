using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _repo = new();
        private readonly Mock<IValidator<CategoryDto>> _validator = new();
        private readonly Mock<ILogger<CategoryService>> _logger = new();

        private CategoryService CreateService() => new(_repo.Object, _validator.Object, _logger.Object);

        [Fact]
        public async Task GetAllWithChildAsync_WhenIncludeSystemFalse_HidesSystemCategoriesButKeepsUncategorized()
        {
            // Transfer and Initial Balance are IsSystem=true and must stay hidden (only Transfer
            // used to get filtered before). Uncategorized is also IsSystem=true but must stay
            // visible — transactions dumped there still need to be reviewed/re-categorized.
            _repo.Setup(x => x.GetAllAsync(true, true)).ReturnsAsync(new List<Category>
            {
                new() { Id = 1, Name = "Food", Type = CategoryTypeEnum.Expense, Icon = "Restaurant", IsSystem = false },
                new() { Id = 2, Name = "Transfer", Type = CategoryTypeEnum.Transfer, Icon = "SwapHoriz", IsSystem = true, SystemCategoryCode = SystemCategoryCodes.TransferOut },
                new() { Id = 3, Name = "Initial Balance", Type = CategoryTypeEnum.Income, Icon = "AccountBalanceWallet", IsSystem = true, SystemCategoryCode = SystemCategoryCodes.InitialBalanceIncome },
                new() { Id = 4, Name = "Uncategorized", Type = CategoryTypeEnum.Expense, Icon = "Help", IsSystem = true, SystemCategoryCode = SystemCategoryCodes.UncategorizedExpense }
            });
            _repo.Setup(x => x.GetUsedCategoryIdsAsync()).ReturnsAsync(new HashSet<int>());
            var svc = CreateService();

            var result = await svc.GetAllWithChildAsync(incluideSystem: false);

            result.Success.Should().BeTrue();
            result.Data!.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 4 });
        }

        [Fact]
        public async Task GetAllWithChildAsync_MarksCategoriesWithoutTransactionsOrBudgetsAsUnused()
        {
            _repo.Setup(x => x.GetAllAsync(true, true)).ReturnsAsync(new List<Category>
            {
                new()
                {
                    Id = 1, Name = "Food", Type = CategoryTypeEnum.Expense, Icon = "Restaurant",
                    Children = new List<Category>
                    {
                        new() { Id = 2, Name = "Groceries", Type = CategoryTypeEnum.Expense, Icon = "Restaurant", ParentId = 1 }
                    }
                },
                new() { Id = 3, Name = "Old Category", Type = CategoryTypeEnum.Expense, Icon = "Payments" }
            });
            _repo.Setup(x => x.GetUsedCategoryIdsAsync()).ReturnsAsync(new HashSet<int> { 1 });
            var svc = CreateService();

            var result = await svc.GetAllWithChildAsync();

            var food = result.Data!.Single(c => c.Id == 1);
            var groceries = food.Children.Single(c => c.Id == 2);
            var oldCategory = result.Data!.Single(c => c.Id == 3);

            food.IsUnused.Should().BeFalse();
            groceries.IsUnused.Should().BeTrue();
            oldCategory.IsUnused.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllWithChildAsync_ParentWithChildrenIsNeverUnused_EvenWithoutOwnUsage()
        {
            // A parent is a grouping bucket, not a leaf a transaction/budget points at directly —
            // it must never be flagged unused (and therefore deletable) just because it has no
            // transactions/budgets of its own.
            _repo.Setup(x => x.GetAllAsync(true, true)).ReturnsAsync(new List<Category>
            {
                new()
                {
                    Id = 1, Name = "Food", Type = CategoryTypeEnum.Expense, Icon = "Restaurant",
                    Children = new List<Category>
                    {
                        new() { Id = 2, Name = "Groceries", Type = CategoryTypeEnum.Expense, Icon = "Restaurant", ParentId = 1 }
                    }
                }
            });
            _repo.Setup(x => x.GetUsedCategoryIdsAsync()).ReturnsAsync(new HashSet<int>());
            var svc = CreateService();

            var result = await svc.GetAllWithChildAsync();

            var food = result.Data!.Single(c => c.Id == 1);
            food.IsUnused.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllWithChildAsync_FlagsCategoriesWithSameNormalizedNameAsPossibleDuplicates()
        {
            _repo.Setup(x => x.GetAllAsync(true, true)).ReturnsAsync(new List<Category>
            {
                new()
                {
                    Id = 1, Name = "Comida", Type = CategoryTypeEnum.Expense, Icon = "Restaurant",
                    Children = new List<Category>
                    {
                        new() { Id = 3, Name = "Super", Type = CategoryTypeEnum.Expense, Icon = "Restaurant", ParentId = 1 },
                        new() { Id = 4, Name = "  super ", Type = CategoryTypeEnum.Expense, Icon = "Restaurant", ParentId = 1 }
                    }
                },
                new() { Id = 2, Name = "Comída", Type = CategoryTypeEnum.Expense, Icon = "Payments" },
                new() { Id = 5, Name = "Transporte", Type = CategoryTypeEnum.Expense, Icon = "Payments" }
            });
            _repo.Setup(x => x.GetUsedCategoryIdsAsync()).ReturnsAsync(new HashSet<int>());
            var svc = CreateService();

            var result = await svc.GetAllWithChildAsync();

            result.Data!.Single(c => c.Id == 1).IsPossibleDuplicate.Should().BeTrue();
            result.Data!.Single(c => c.Id == 2).IsPossibleDuplicate.Should().BeTrue();
            result.Data!.Single(c => c.Id == 5).IsPossibleDuplicate.Should().BeFalse();

            var food = result.Data!.Single(c => c.Id == 1);
            food.Children.Single(c => c.Id == 3).IsPossibleDuplicate.Should().BeTrue();
            food.Children.Single(c => c.Id == 4).IsPossibleDuplicate.Should().BeTrue();
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsFailResult()
        {
            _repo.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Category?)null);
            var svc = CreateService();

            var result = await svc.GetByIdAsync(99);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task CreateAsync_WhenValidationFails_ReturnsFirstValidationError()
        {
            // Service should short-circuit when validation fails and skip repository calls.
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CategoryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));
            var svc = CreateService();

            var result = await svc.CreateAsync(new CategoryDto());

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Name is required");
            _repo.Verify(x => x.AddAsync(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenDuplicateName_ReturnsAlreadyExists()
        {
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CategoryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _repo.Setup(x => x.ExistsAsync("Food", null)).ReturnsAsync(true);
            var svc = CreateService();

            var result = await svc.CreateAsync(new CategoryDto { Name = "Food", Type = CategoryTypeEnum.Expense, Icon = CategoryIcon.Payments });

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.AlreadyExists);
        }

        [Fact]
        public async Task CreateAsync_WhenValid_AddsCategoryAndReturnsCreated()
        {
            // Happy path: valid payload and non-duplicate name should be persisted.
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CategoryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _repo.Setup(x => x.ExistsAsync("Food", null)).ReturnsAsync(false);
            var svc = CreateService();
            var dto = new CategoryDto { Name = "Food", Type = CategoryTypeEnum.Expense, Icon = CategoryIcon.Payments };

            var result = await svc.CreateAsync(dto);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Created);
            _repo.Verify(x => x.AddAsync(It.Is<Category>(c => c.Name == "Food")), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ReturnsFailResult()
        {
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CategoryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Category?)null);
            var svc = CreateService();

            var result = await svc.UpdateAsync(new CategoryDto { Id = 1, Name = "Updated", Icon = CategoryIcon.Payments });

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task MergeAsync_WhenSameId_ReturnsFailWithoutHittingRepository()
        {
            var svc = CreateService();

            var result = await svc.MergeAsync(1, 1, new List<int>(), true);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.CategoryMergeSelf);
            _repo.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task MergeAsync_WhenSourceNotFound_ReturnsNotFound()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Category?)null);
            var svc = CreateService();

            var result = await svc.MergeAsync(1, 2, new List<int>(), true);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task MergeAsync_WhenDifferentType_ReturnsFail()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Food", Type = CategoryTypeEnum.Expense });
            _repo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Category { Id = 2, Name = "Salary", Type = CategoryTypeEnum.Income });
            var svc = CreateService();

            var result = await svc.MergeAsync(1, 2, new List<int>(), true);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.CategoryMergeDifferentType);
            _repo.Verify(x => x.MergeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task MergeAsync_WhenSystemCategoryInvolved_ReturnsFail()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Food", Type = CategoryTypeEnum.Expense, IsSystem = true });
            _repo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Category { Id = 2, Name = "Groceries", Type = CategoryTypeEnum.Expense });
            var svc = CreateService();

            var result = await svc.MergeAsync(1, 2, new List<int>(), true);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.CategoryMergeSystem);
        }

        [Fact]
        public async Task MergeAsync_WhenSourceIsUncategorized_AllowsMerge()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category
            {
                Id = 1,
                Name = "Uncategorized",
                Type = CategoryTypeEnum.Expense,
                IsSystem = true,
                SystemCategoryCode = SystemCategoryCodes.UncategorizedExpense
            });
            _repo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Category { Id = 2, Name = "Groceries", Type = CategoryTypeEnum.Expense });
            _repo.Setup(x => x.MergeAsync(1, 2, It.IsAny<List<int>>(), true)).Returns(Task.CompletedTask);
            var svc = CreateService();

            var result = await svc.MergeAsync(1, 2, new List<int>(), true);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task MergeAsync_WhenTargetIsUncategorized_AllowsMerge()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Groceries", Type = CategoryTypeEnum.Expense });
            _repo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Category
            {
                Id = 2,
                Name = "Uncategorized",
                Type = CategoryTypeEnum.Expense,
                IsSystem = true,
                SystemCategoryCode = SystemCategoryCodes.UncategorizedExpense
            });
            _repo.Setup(x => x.MergeAsync(1, 2, It.IsAny<List<int>>(), true)).Returns(Task.CompletedTask);
            var svc = CreateService();

            var result = await svc.MergeAsync(1, 2, new List<int>(), true);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task MergeAsync_WhenFullMerge_MergesAndReturnsSuccess()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Old Food", Type = CategoryTypeEnum.Expense });
            _repo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Category { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense });
            var selectedIds = new List<int> { 10, 11 };
            _repo.Setup(x => x.MergeAsync(1, 2, selectedIds, true)).Returns(Task.CompletedTask);
            var svc = CreateService();

            var result = await svc.MergeAsync(1, 2, selectedIds, true);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.CategoryMerged);
            _repo.Verify(x => x.MergeAsync(1, 2, selectedIds, true), Times.Once);
        }

        [Fact]
        public async Task MergeAsync_WithSelectedTransactionIds_PassesThemToRepository()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Old Food", Type = CategoryTypeEnum.Expense });
            _repo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Category { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense });
            var selectedIds = new List<int> { 10, 11 };
            _repo.Setup(x => x.MergeAsync(1, 2, selectedIds, true)).Returns(Task.CompletedTask);
            var svc = CreateService();

            var result = await svc.MergeAsync(1, 2, selectedIds, true);

            result.Success.Should().BeTrue();
            _repo.Verify(x => x.MergeAsync(1, 2, selectedIds, true), Times.Once);
        }

        [Fact]
        public async Task MergeAsync_WhenPartialMove_ReturnsPartialMessageAndKeepsSourceCategory()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Old Food", Type = CategoryTypeEnum.Expense });
            _repo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Category { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense });
            var selectedIds = new List<int> { 10 };
            _repo.Setup(x => x.MergeAsync(1, 2, selectedIds, false)).Returns(Task.CompletedTask);
            var svc = CreateService();

            var result = await svc.MergeAsync(1, 2, selectedIds, false);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.CategoryPartiallyMoved);
            _repo.Verify(x => x.MergeAsync(1, 2, selectedIds, false), Times.Once);
        }

        [Fact]
        public async Task GetMergePreviewAsync_ReturnsBudgetsAndSubcategoryNames()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category
            {
                Id = 1,
                Name = "Old Food",
                Type = CategoryTypeEnum.Expense,
                Children = new List<Category>
                {
                    new() { Id = 3, Name = "Fast Food" },
                    new() { Id = 4, Name = "Snacks" }
                }
            });
            _repo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Category { Id = 2, Name = "Food", Type = CategoryTypeEnum.Expense });
            _repo.Setup(x => x.GetBudgetMergePreviewAsync(1, 2)).ReturnsAsync(new List<(int Year, int Month, decimal Amount, bool WillBeDropped)>
            {
                (2026, 1, 100m, false),
                (2026, 2, 50m, true)
            });

            var svc = CreateService();

            var result = await svc.GetMergePreviewAsync(1, 2);

            result.Success.Should().BeTrue();
            result.Data!.SubcategoryNames.Should().BeEquivalentTo(new[] { "Fast Food", "Snacks" });
            result.Data.Budgets.Should().HaveCount(2);
            result.Data.Budgets.Should().ContainSingle(b => b.Month == 2 && b.WillBeDropped);
            result.Data.Budgets.Should().ContainSingle(b => b.Month == 1 && !b.WillBeDropped);
        }

        [Fact]
        public async Task GetMergePreviewAsync_WhenDifferentType_ReturnsFail()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Food", Type = CategoryTypeEnum.Expense });
            _repo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Category { Id = 2, Name = "Salary", Type = CategoryTypeEnum.Income });
            var svc = CreateService();

            var result = await svc.GetMergePreviewAsync(1, 2);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.CategoryMergeDifferentType);
        }

        [Fact]
        public async Task DeleteAsync_WhenRepositoryThrows_ReturnsUnexpectedError()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, IsSystem = false });
            _repo.Setup(x => x.HasBudgetsAsync(1)).ReturnsAsync(false);
            _repo.Setup(x => x.DeleteAsync(1)).ThrowsAsync(new Exception("db error"));
            var svc = CreateService();

            var result = await svc.DeleteAsync(1);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.UnexpectedError);
        }

        [Fact]
        public async Task DeleteAsync_WhenCategoryHasBudgets_ReturnsFailAndSkipsDelete()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, IsSystem = false });
            _repo.Setup(x => x.HasBudgetsAsync(1)).ReturnsAsync(true);
            var svc = CreateService();

            var result = await svc.DeleteAsync(1);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.CategoryHasBudgets);
            _repo.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenCategoryNotFound_ReturnsNotFound()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Category?)null);
            var svc = CreateService();

            var result = await svc.DeleteAsync(1);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
            _repo.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenCategoryIsSystem_ReturnsFailAndSkipsDelete()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, IsSystem = true });
            var svc = CreateService();

            var result = await svc.DeleteAsync(1);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.CategoryDeleteSystem);
            _repo.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenCategoryHasChildren_ReturnsFailAndSkipsDelete()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category
            {
                Id = 1,
                IsSystem = false,
                Children = new List<Category> { new() { Id = 2, Name = "Sub" } }
            });
            var svc = CreateService();

            var result = await svc.DeleteAsync(1);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.CategoryHasChildren);
            _repo.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
            _repo.Verify(x => x.HasBudgetsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenNoBudgets_DeletesAndReturnsSuccess()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, IsSystem = false });
            _repo.Setup(x => x.HasBudgetsAsync(1)).ReturnsAsync(false);
            _repo.Setup(x => x.DeleteAsync(1)).Returns(Task.CompletedTask);
            var svc = CreateService();

            var result = await svc.DeleteAsync(1);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Deleted);
            _repo.Verify(x => x.DeleteAsync(1), Times.Once);
        }
    }
}
