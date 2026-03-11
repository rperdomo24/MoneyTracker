using FluentValidation;
using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Validators.Auth
{
    public class AcceptInvitationRequestValidator : AbstractValidator<AcceptInvitationRequestDto>
    {
        public AcceptInvitationRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Invitation token is required.");

            RuleFor(x => x.DisplayName)
                .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");

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
