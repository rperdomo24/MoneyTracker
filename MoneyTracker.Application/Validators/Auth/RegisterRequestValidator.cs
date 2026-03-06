using FluentValidation;
using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Validators.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.DisplayName)
                .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(10).WithMessage("Password must be at least 10 characters.")
                .Matches("[A-Z]").WithMessage("Password must include at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must include at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must include at least one number.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Password and confirmation do not match.");
        }
    }
}
