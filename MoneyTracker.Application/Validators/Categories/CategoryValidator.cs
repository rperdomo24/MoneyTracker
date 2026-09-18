using FluentValidation;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Validators.Categories
{
    public class CategoryValidator : AbstractValidator<CategoryDto>
    {
        public CategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidationMessages.NameRequired)
                .MaximumLength(100).WithMessage(ValidationMessages.NameMaxLength);

            RuleFor(x => x.Icon)
                .IsInEnum().WithMessage(ValidationMessages.IconMaxLength);

            RuleFor(x => x.Color)
                .MaximumLength(10).WithMessage(string.Format(ValidationMessages.ColorMaxLength, 10))
                .When(a => !string.IsNullOrWhiteSpace(a.Color));
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<CategoryDto>.CreateWithOptions((CategoryDto)model, x => x.IncludeProperties(propertyName)));
            if (result.IsValid)
                return Array.Empty<string>();
            return result.Errors.Select(e => e.ErrorMessage);
        };
    }
}
