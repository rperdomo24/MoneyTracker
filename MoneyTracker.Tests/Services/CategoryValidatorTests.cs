using FluentAssertions;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Validators.Categories;
using MoneyTracker.Domain.Enums.Category;

namespace MoneyTracker.Tests.Services
{
    public class CategoryValidatorTests
    {
        private readonly CategoryValidator _validator = new();

        private static CategoryDto ValidCategory(string name) => new()
        {
            Name = name,
            Type = CategoryTypeEnum.Expense,
            Icon = CategoryIcon.Payments
        };

        [Fact]
        public async Task Validate_WhenNameIsClean_IsValid()
        {
            var result = await _validator.ValidateAsync(ValidCategory("Gastos Extras"));

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(" Gastos Extras")]
        [InlineData("Gastos Extras ")]
        [InlineData("\tGastos Extras")]
        public async Task Validate_WhenNameHasOuterWhitespace_Fails(string name)
        {
            var result = await _validator.ValidateAsync(ValidCategory(name));

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().Contain(ValidationMessages.NameNoOuterSpaces);
        }

        [Fact]
        public async Task Validate_WhenNameIsEmpty_FailsWithRequiredOnly()
        {
            var result = await _validator.ValidateAsync(ValidCategory(string.Empty));

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().Contain(ValidationMessages.NameRequired);
            result.Errors.Select(e => e.ErrorMessage).Should().NotContain(ValidationMessages.NameNoOuterSpaces);
        }
    }
}
