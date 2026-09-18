using FluentValidation;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.CardBenefits;

namespace MoneyTracker.Application.Validators.CardBenefits
{
    public class CardBenefitValidator : AbstractValidator<CardBenefitDto>
    {
        public CardBenefitValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0).WithMessage(ValidationMessages.AccountRequired);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidationMessages.Required)
                .MaximumLength(100).WithMessage(string.Format(ValidationMessages.MaxLength, 100));

            RuleFor(x => x.MerchantPattern)
                .MaximumLength(200).WithMessage(string.Format(ValidationMessages.MaxLength, 200))
                .When(x => !string.IsNullOrWhiteSpace(x.MerchantPattern));

            RuleFor(x => x.BenefitType)
                .IsInEnum().WithMessage(ValidationMessages.TypeRequired);

            RuleFor(x => x.BenefitRate)
                .GreaterThan(0).WithMessage(ValidationMessages.GreaterThanZero)
                .LessThanOrEqualTo(1).WithMessage("Benefit rate must be between 0 and 1 (e.g. 0.05 = 5%).");

            RuleFor(x => x.MaxMonthlyCap)
                .GreaterThan(0).WithMessage(ValidationMessages.GreaterThanZero)
                .When(x => x.MaxMonthlyCap.HasValue);
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<CardBenefitDto>.CreateWithOptions(
                (CardBenefitDto)model, x => x.IncludeProperties(propertyName)));
            if (result.IsValid)
                return Array.Empty<string>();
            return result.Errors.Select(e => e.ErrorMessage);
        };
    }
}
