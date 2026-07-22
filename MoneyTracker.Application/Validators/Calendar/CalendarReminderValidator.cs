using FluentValidation;
using MoneyTracker.Application.DTOs.Calendar;

namespace MoneyTracker.Application.Validators.Calendar
{
    public class CalendarReminderValidator : AbstractValidator<CalendarReminderDto>
    {
        public CalendarReminderValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required.");

            RuleFor(x => x.Color)
                .NotEmpty().WithMessage("Color is required.")
                .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color.");

            RuleFor(x => x.RecurrenceFrequency)
                .NotNull().When(x => x.IsRecurring)
                .WithMessage("Recurrence frequency is required for recurring reminders.");

            RuleFor(x => x.RecurrenceEndDate)
                .GreaterThan(x => x.Date).When(x => x.IsRecurring && x.RecurrenceEndDate.HasValue)
                .WithMessage("Recurrence end date must be after the reminder date.");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).When(x => x.Notes is not null)
                .WithMessage("Notes cannot exceed 1000 characters.");
        }
    }
}
