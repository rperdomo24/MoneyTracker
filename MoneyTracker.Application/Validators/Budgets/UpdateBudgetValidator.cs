using FluentValidation;
using MoneyTracker.Application.DTOs.Budgets;

namespace MoneyTracker.Application.Validators.Budgets
{
    public class UpdateBudgetValidator : AbstractValidator<UpdateBudgetDto>
    {
        public UpdateBudgetValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        }
    }
}
