using MoneyTracker.Application.DTOs.Loans;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Loans;

namespace MoneyTracker.Application.Mappers
{
    public static class LoanMapper
    {
        public static LoanDto MapToDto(this Loan loan)
        {
            var interestAmount = Math.Round(loan.PrincipalAmount * loan.InterestRate / 100, 2);
            var totalOwed = loan.PrincipalAmount + interestAmount;
            var totalPaid = loan.Payments.Sum(p => p.Amount);
            var balance = Math.Max(0, totalOwed - totalPaid);
            var overpayment = Math.Max(0, totalPaid - totalOwed);
            var progress = totalOwed > 0
                ? Math.Min(100, Math.Round(totalPaid / totalOwed * 100, 1))
                : 100m;
            var isOverdue = loan.Status == LoanStatus.Active
                && loan.DueDate.HasValue
                && loan.DueDate.Value.Date < DateTime.UtcNow.Date;
            var monthlyQuota = loan.NumberOfInstallments is > 0
                ? Math.Round(totalOwed / loan.NumberOfInstallments.Value, 2)
                : 0m;

            var installmentDtos = BuildInstallmentDtos(loan, totalPaid);

            return new LoanDto
            {
                Id = loan.Id,
                ContactName = loan.ContactName,
                Description = loan.Description,
                PrincipalAmount = loan.PrincipalAmount,
                InterestRate = loan.InterestRate,
                NumberOfInstallments = loan.NumberOfInstallments,
                FirstPaymentDate = loan.FirstPaymentDate,
                StartDate = loan.StartDate,
                DueDate = loan.DueDate,
                Status = loan.Status,
                Notes = loan.Notes,
                InterestAmount = interestAmount,
                TotalOwed = totalOwed,
                TotalPaid = totalPaid,
                Balance = balance,
                OverpaymentAmount = overpayment,
                IsOverdue = isOverdue,
                ProgressPercent = progress,
                MonthlyQuota = monthlyQuota,
                Payments = loan.Payments
                    .OrderByDescending(p => p.Date)
                    .Select(p => p.MapToDto())
                    .ToList(),
                Installments = installmentDtos
            };
        }

        private static List<LoanInstallmentDto> BuildInstallmentDtos(Loan loan, decimal totalPaid)
        {
            if (!loan.Installments.Any())
                return new List<LoanInstallmentDto>();

            var sorted = loan.Installments
                .OrderBy(i => i.InstallmentNumber)
                .ToList();

            var today = DateTime.UtcNow.Date;
            var result = new List<LoanInstallmentDto>();

            // Allocate total paid cumulatively across installments in order.
            // Each installment gets: min(remaining pot, expectedAmount)
            var remainingPot = totalPaid;

            foreach (var inst in sorted)
            {
                var paid = Math.Min(remainingPot, inst.ExpectedAmount);
                remainingPot -= paid;
                var pending = inst.ExpectedAmount - paid;

                result.Add(new LoanInstallmentDto
                {
                    Id = inst.Id,
                    LoanId = inst.LoanId,
                    InstallmentNumber = inst.InstallmentNumber,
                    DueDate = inst.DueDate,
                    ExpectedAmount = inst.ExpectedAmount,
                    Notes = inst.Notes,
                    PaidAmount = paid,
                    PendingAmount = pending,
                    IsPaid = pending <= 0,
                    IsPartiallyPaid = paid > 0 && pending > 0,
                    IsOverdue = pending > 0 && inst.DueDate.Date < today
                });
            }

            return result;
        }

        public static LoanPaymentDto MapToDto(this LoanPayment payment) => new()
        {
            Id = payment.Id,
            LoanId = payment.LoanId,
            TransactionId = payment.TransactionId,
            TransactionName = payment.Transaction?.Name,
            TransactionDescription = payment.Transaction?.Description,
            TransactionDate = payment.Transaction?.Date,
            TransactionAmount = payment.Transaction?.Amount,
            TransactionAccount = payment.Transaction?.Account?.Name,
            Amount = payment.Amount,
            Date = payment.Date,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt
        };

        public static Loan MapToEntity(this CreateLoanDto dto) => new()
        {
            ContactName = dto.ContactName.Trim(),
            Description = dto.Description?.Trim(),
            PrincipalAmount = dto.PrincipalAmount,
            InterestRate = dto.InterestRate,
            NumberOfInstallments = dto.NumberOfInstallments,
            FirstPaymentDate = dto.FirstPaymentDate.HasValue
                ? DateTime.SpecifyKind(dto.FirstPaymentDate.Value, DateTimeKind.Utc)
                : null,
            StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc),
            DueDate = dto.DueDate.HasValue
                ? DateTime.SpecifyKind(dto.DueDate.Value, DateTimeKind.Utc)
                : null,
            Notes = dto.Notes?.Trim(),
            Status = LoanStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        public static void UpdateEntity(this Loan loan, LoanDto dto)
        {
            loan.ContactName = dto.ContactName.Trim();
            loan.Description = dto.Description?.Trim();
            loan.PrincipalAmount = dto.PrincipalAmount;
            loan.InterestRate = dto.InterestRate;
            loan.NumberOfInstallments = dto.NumberOfInstallments;
            loan.FirstPaymentDate = dto.FirstPaymentDate.HasValue
                ? DateTime.SpecifyKind(dto.FirstPaymentDate.Value, DateTimeKind.Utc)
                : null;
            loan.StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
            loan.DueDate = dto.DueDate.HasValue
                ? DateTime.SpecifyKind(dto.DueDate.Value, DateTimeKind.Utc)
                : null;
            loan.Status = dto.Status;
            loan.Notes = dto.Notes?.Trim();
            loan.UpdatedAt = DateTime.UtcNow;
        }

        public static List<LoanInstallment> GenerateInstallments(Loan loan)
        {
            if (loan.NumberOfInstallments is not > 0 || loan.FirstPaymentDate is null)
                return new List<LoanInstallment>();

            var totalOwed = loan.PrincipalAmount + Math.Round(loan.PrincipalAmount * loan.InterestRate / 100, 2);
            var baseQuota = Math.Round(totalOwed / loan.NumberOfInstallments!.Value, 2);
            // Last installment absorbs rounding difference
            var lastQuota = totalOwed - baseQuota * (loan.NumberOfInstallments.Value - 1);

            var installments = new List<LoanInstallment>();
            var firstDue = DateTime.SpecifyKind(loan.FirstPaymentDate.Value, DateTimeKind.Utc);

            for (int i = 1; i <= loan.NumberOfInstallments.Value; i++)
            {
                var dueDate = firstDue.AddMonths(i - 1);
                installments.Add(new LoanInstallment
                {
                    LoanId = loan.Id,
                    InstallmentNumber = i,
                    DueDate = dueDate,
                    ExpectedAmount = i == loan.NumberOfInstallments.Value ? lastQuota : baseQuota,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            return installments;
        }
    }
}
