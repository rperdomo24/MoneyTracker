using FluentValidation;
using MoneyTracker.Application.DTOs.Gmail;

namespace MoneyTracker.Application.Validators.Gmail
{
    public class EmailImportRuleValidator : AbstractValidator<EmailImportRuleDto>
    {
        public EmailImportRuleValidator()
        {
            RuleFor(x => x.SenderPattern)
                .NotEmpty().WithMessage("Sender or domain is required.")
                .MaximumLength(320).WithMessage("Sender pattern cannot exceed 320 characters.");

            RuleFor(x => x.BankLabel)
                .NotEmpty().WithMessage("Bank label is required.")
                .MaximumLength(100).WithMessage("Bank label cannot exceed 100 characters.");

            RuleFor(x => x.SubjectIncludeKeywords)
                .MaximumLength(500).When(x => x.SubjectIncludeKeywords is not null)
                .WithMessage("Subject include keywords cannot exceed 500 characters.");

            RuleFor(x => x.SubjectExcludeKeywords)
                .MaximumLength(500).When(x => x.SubjectExcludeKeywords is not null)
                .WithMessage("Subject exclude keywords cannot exceed 500 characters.");
        }
    }
}
