using FluentValidation;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Validators
{

    public class AccountValidator : AbstractValidator<AccountDto>
    {
        public AccountValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidationMessages.Required)
                .MaximumLength(100).WithMessage(string.Format(ValidationMessages.MaxLength, 100));

            RuleFor(x => x.Type)
                .MaximumLength(50).WithMessage(string.Format(ValidationMessages.MaxLength, 50))
                .When(x => !string.IsNullOrWhiteSpace(x.Type));

            RuleFor(x => x.Color)
                .MaximumLength(10).WithMessage(string.Format(ValidationMessages.MaxLength, 10))
                .When(x => !string.IsNullOrWhiteSpace(x.Color));

            RuleFor(x => x.Notes)
                .MaximumLength(255).WithMessage(string.Format(ValidationMessages.MaxLength, 255))
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0).WithMessage("Balance cannot be negative.");

            RuleFor(x => x.CreditLimit)
                .GreaterThanOrEqualTo(0).WithMessage("Credit limit cannot be negative.");
        }
    }
}
