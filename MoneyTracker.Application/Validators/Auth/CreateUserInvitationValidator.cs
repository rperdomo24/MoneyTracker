using FluentValidation;
using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Validators.Auth
{
    public class CreateUserInvitationValidator : AbstractValidator<CreateUserInvitationDto>
    {
        public CreateUserInvitationValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.");

            RuleFor(x => x.InviteUrlTemplate)
                .NotEmpty().WithMessage("Invite URL template is required.")
                .Must(x => x.Contains("{token}", StringComparison.Ordinal))
                .WithMessage("Invite URL template must include the {token} placeholder.");
        }
    }
}
