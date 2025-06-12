using FluentValidation;
using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.Application.Validators.Transaction
{
    public class TransactionValidator : AbstractValidator<TransactionDto>
    {

        public TransactionValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Please enter a name for the transaction.")
                .MaximumLength(100).WithMessage("The name cannot exceed 100 characters.");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Please select a date.")
                .LessThanOrEqualTo(DateTime.Today).WithMessage("The date cannot be in the future.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("The amount must be greater than zero.");

            RuleFor(x => x.Description)
                .MaximumLength(255).WithMessage("The description cannot exceed 255 characters.");
        }
    }
}
