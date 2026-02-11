using FluentValidation;
using MoneyTracker.Application.DTOs.Budgets;

namespace MoneyTracker.Application.Validators.Budgets
{
    public class CreateBudgetValidator : AbstractValidator<CreateBudgetDto>
    {
        public CreateBudgetValidator()
        {
            RuleFor(x => x.CategoryId).GreaterThan(0);
            RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
            RuleFor(x => x.Month).InclusiveBetween(1, 12);
            RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        }
    }
}
