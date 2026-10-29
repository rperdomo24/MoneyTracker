using FluentAssertions;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Validators.Accounts;
using MoneyTracker.Domain.Enums.Account;

namespace MoneyTracker.Tests.Services
{
    public class AccountValidatorTests
    {
        private readonly AccountValidator _validator = new();

        private static AccountDto ValidAccount(string name) => new()
        {
            Name = name,
            Type = AccountType.Cash,
            Icon = AccountIcon.Wallet
        };

        [Fact]
        public async Task Validate_WhenNameIsClean_IsValid()
        {
            var result = await _validator.ValidateAsync(ValidAccount("BAC - VISA BLACK - 2603"));

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(" BAC - VISA")]
        [InlineData("BAC - VISA ")]
        [InlineData("\tBAC - VISA")]
        public async Task Validate_WhenNameHasOuterWhitespace_Fails(string name)
        {
            var result = await _validator.ValidateAsync(ValidAccount(name));

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().Contain(ValidationMessages.NameNoOuterSpaces);
        }

        [Fact]
        public async Task Validate_WhenNameIsEmpty_FailsWithRequiredOnly()
        {
            var result = await _validator.ValidateAsync(ValidAccount(string.Empty));

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().NotContain(ValidationMessages.NameNoOuterSpaces);
        }
    }
}
