using FluentValidation;
using MoneyTracker.Application.DTOs.Auth;
using System.Linq;

namespace MoneyTracker.Application.Validators.Auth
{
    public class LoginOtpRequestValidator : AbstractValidator<LoginOtpRequestDto>
    {
        public LoginOtpRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Verification code is required.")
                .Must(BeSixDigits).WithMessage("Verification code must be 6 digits.");
        }

        private static bool BeSixDigits(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var normalized = code.Replace(" ", string.Empty).Replace("-", string.Empty);
            return normalized.Length == 6 && normalized.All(char.IsDigit);
        }
    }
}
