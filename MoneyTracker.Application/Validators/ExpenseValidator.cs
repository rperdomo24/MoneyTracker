using FluentValidation;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Validators
{
    public class ExpenseValidator : AbstractValidator<ExpenseDto>
    {
        public ExpenseValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must be at most 100 characters.");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required.")
                .LessThanOrEqualTo(DateTime.Today).WithMessage("Date cannot be in the future.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.Description)
                .MaximumLength(255).WithMessage("Description must be at most 255 characters.");

            RuleFor(x => x.PaymentMethod)
                .MaximumLength(50).WithMessage("Payment method must be at most 50 characters.");
        }
    }
}
