using FluentValidation;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Validators
{

    public class IncomeValidator : AbstractValidator<IncomeDto>
    {
        public IncomeValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidationMessages.Required)
                .MaximumLength(100).WithMessage(string.Format(ValidationMessages.MaxLength, 100));

            RuleFor(x => x.Date)
                .LessThanOrEqualTo(DateTime.Today).WithMessage(ValidationMessages.DateNotInFuture);

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage(ValidationMessages.GreaterThanZero);

            RuleFor(x => x.Description)
                .MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage(string.Format(ValidationMessages.MaxLength, 255));

            RuleFor(x => x.PaymentMethod)
                .MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod))
                .WithMessage(string.Format(ValidationMessages.MaxLength, 50));
        }
    }
}
