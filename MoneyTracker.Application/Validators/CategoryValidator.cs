using FluentValidation;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Validators
{
    public class CategoryValidator : AbstractValidator<CategoryDto>
    {
        public CategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidationMessages.NameRequired)
                .MaximumLength(100).WithMessage(ValidationMessages.NameMaxLength);

            RuleFor(x => x.Icon)
                .MaximumLength(50).WithMessage(ValidationMessages.IconMaxLength);

            RuleFor(x => x.Color)
                .MaximumLength(10).WithMessage(ValidationMessages.ColorMaxLength);
        }
    }
}
