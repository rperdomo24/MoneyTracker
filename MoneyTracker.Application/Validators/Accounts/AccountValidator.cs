using FluentValidation;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Validators.Accounts
{
    public class AccountValidator : AbstractValidator<AccountDto>
    {
        public AccountValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidationMessages.Required)
                .MaximumLength(100).WithMessage(string.Format(ValidationMessages.MaxLength, 100));

            RuleFor(a => a.CurrentBalance)
                .GreaterThanOrEqualTo(0)
                .WithMessage(ValidationMessages.AmountNotNegative);

            RuleFor(a => a.CreditLimit)
                .GreaterThanOrEqualTo(0)
                .WithMessage(ValidationMessages.AmountNotNegative);

            RuleFor(a => a.Type)
           .IsInEnum().WithMessage(ValidationMessages.TypeRequired);

            RuleFor(a => a.Icon)
           .IsInEnum()
           .WithMessage(ValidationMessages.TypeRequired);

            RuleFor(x => x.Color)
                .MaximumLength(10).WithMessage(string.Format(ValidationMessages.ColorMaxLength, 10))
                .Matches("^#(?:[0-9a-fA-F]{3}){1,2}$")
                .When(a => !string.IsNullOrWhiteSpace(a.Color));

            RuleFor(x => x.Notes)
                .MaximumLength(255).WithMessage(string.Format(ValidationMessages.MaxLength, 255))
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));

            RuleFor(x => x.BankName)
                .MaximumLength(100).WithMessage(string.Format(ValidationMessages.MaxLength, 100))
                .When(x => !string.IsNullOrWhiteSpace(x.BankName));

            RuleFor(x => x.CardDisplayName)
                .MaximumLength(150).WithMessage(string.Format(ValidationMessages.MaxLength, 150))
                .When(x => !string.IsNullOrWhiteSpace(x.CardDisplayName));
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<AccountDto>.CreateWithOptions((AccountDto)model, x => x.IncludeProperties(propertyName)));
            if (result.IsValid)
                return Array.Empty<string>();
            return result.Errors.Select(e => e.ErrorMessage);
        };
    }
}
