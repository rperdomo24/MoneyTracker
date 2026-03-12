using FluentValidation;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.Application.Validators.Transaction
{
    public class TransactionFilterValidator : AbstractValidator<TransactionFilterDto>
    {
        public TransactionFilterValidator()
        {
            RuleFor(x => x.TimePeriod)
                .IsInEnum()
                .WithMessage("Período de tiempo inválido.");

            When(x => x.TimePeriod == TimePeriodFilter.Custom, () =>
            {
                RuleFor(x => x.FromDate)
                    .NotNull()
                    .WithMessage("Fecha de inicio es requerida para período personalizado.");

                RuleFor(x => x.ToDate)
                    .NotNull()
                    .WithMessage("Fecha de fin es requerida para período personalizado.");

                RuleFor(x => x.ToDate)
                    .GreaterThanOrEqualTo(x => x.FromDate)
                    .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
                    .WithMessage("La fecha de fin debe ser mayor o igual a la fecha de inicio.");
            });

            RuleForEach(x => x.AccountIds)
                .GreaterThan(0)
                .WithMessage("ID de cuenta inválido.");

            RuleForEach(x => x.TransactionTypeIds)
                .GreaterThan(0)
                .WithMessage("ID de tipo de transacción inválido.");
        }
    }
}
