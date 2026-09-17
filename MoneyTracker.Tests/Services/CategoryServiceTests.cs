using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Services;
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
        public async Task GetAllWithChildAsync_WhenIncludeSystemFalse_FiltersTransferType()
        {
            // Include transfer and non-transfer categories to verify filtering behavior.
            _repo.Setup(x => x.GetAllAsync(true, true)).ReturnsAsync(new List<Category>
            {
                new() { Id = 1, Name = "Food", Type = CategoryTypeEnum.Expense, Icon = "Restaurant" },
                new() { Id = 2, Name = "Transfer", Type = CategoryTypeEnum.Transfer, Icon = "SwapHoriz" }
            });
            _repo.Setup(x => x.GetUsedCategoryIdsAsync()).ReturnsAsync(new HashSet<int>());
            var svc = CreateService();

            var result = await svc.GetAllWithChildAsync(incluideSystem: false);

            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle(x => x.Type == CategoryTypeEnum.Expense);
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
        public async Task DeleteAsync_WhenRepositoryThrows_ReturnsUnexpectedError()
        {
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
            _repo.Setup(x => x.HasBudgetsAsync(1)).ReturnsAsync(true);
            var svc = CreateService();

            var result = await svc.DeleteAsync(1);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.CategoryHasBudgets);
            _repo.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenNoBudgets_DeletesAndReturnsSuccess()
        {
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
