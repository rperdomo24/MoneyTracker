using FluentValidation;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.Application.Validators.Transaction
{
    public class CreateTransferValidator : AbstractValidator<CreateTransferDto>
    {
        public CreateTransferValidator()
        {
            RuleFor(x => x.FromAccountId)
                .GreaterThan(0)
                .WithMessage(ValidationMessages.SourceAccountRequired);

            RuleFor(x => x.ToAccountId)
                .GreaterThan(0)
                .WithMessage(ValidationMessages.DestinationAccountRequired);

            RuleFor(x => x.ToAccountId)
                .NotEqual(x => x.FromAccountId)
                .WithMessage(ValidationMessages.SameAccountTransfer);

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage(ValidationMessages.GreaterThanZero);

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage(ValidationMessages.DateRequired);

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage(ValidationMessages.DescriptionMaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.Description));
        }
    }
}
