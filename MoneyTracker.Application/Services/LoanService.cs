using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Loans;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Loans;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _repo;
        private readonly ILogger<LoanService> _logger;

        public LoanService(ILoanRepository repo, ILogger<LoanService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<OperationResult<List<LoanDto>>> GetAllAsync()
        {
            try
            {
                var loans = await _repo.GetAllAsync();
                return OperationResult<List<LoanDto>>.Ok(loans.Select(l => l.MapToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<LoanDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<LoanDto>> GetByIdAsync(int id)
        {
            try
            {
                var loan = await _repo.GetByIdAsync(id);
                if (loan is null)
                    return OperationResult<LoanDto>.Fail(OperationMessages.NotFound);

                return OperationResult<LoanDto>.Ok(loan.MapToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<LoanDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> CreateAsync(CreateLoanDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ContactName))
                    return OperationResult.Fail("Contact name is required.");

                if (dto.PrincipalAmount <= 0)
                    return OperationResult.Fail("Amount must be greater than zero.");

                if (dto.InterestRate < 0)
                    return OperationResult.Fail("Interest rate cannot be negative.");

                if (dto.NumberOfInstallments is < 1)
                    return OperationResult.Fail("Number of installments must be at least 1.");

                if (dto.NumberOfInstallments is > 0 && dto.FirstPaymentDate is null)
                    return OperationResult.Fail("First payment date is required when installments are set.");

                var entity = dto.MapToEntity();
                await _repo.AddAsync(entity);

                if (entity.NumberOfInstallments is > 0)
                {
                    var installments = LoanMapper.GenerateInstallments(entity);
                    if (installments.Any())
                        await _repo.AddInstallmentsAsync(installments);
                }

                return OperationResult.Ok(OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> UpdateAsync(LoanDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ContactName))
                    return OperationResult.Fail("Contact name is required.");

                if (dto.PrincipalAmount <= 0)
                    return OperationResult.Fail("Amount must be greater than zero.");

                if (dto.InterestRate < 0)
                    return OperationResult.Fail("Interest rate cannot be negative.");

                var entity = await _repo.GetByIdAsync(dto.Id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                var installmentsChanged =
                    entity.NumberOfInstallments != dto.NumberOfInstallments ||
                    entity.FirstPaymentDate != dto.FirstPaymentDate ||
                    entity.PaymentFrequency != dto.PaymentFrequency ||
                    entity.PrincipalAmount != dto.PrincipalAmount ||
                    entity.InterestRate != dto.InterestRate;

                entity.UpdateEntity(dto);
                await _repo.UpdateAsync(entity);

                if (installmentsChanged && entity.NumberOfInstallments is > 0 && entity.FirstPaymentDate is not null)
                {
                    await _repo.DeleteInstallmentsByLoanAsync(entity.Id);
                    var installments = LoanMapper.GenerateInstallments(entity);
                    if (installments.Any())
                        await _repo.AddInstallmentsAsync(installments);
                }
                else if (installmentsChanged && (entity.NumberOfInstallments is null or 0))
                {
                    await _repo.DeleteInstallmentsByLoanAsync(entity.Id);
                }

                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            try
            {
                var entity = await _repo.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                await _repo.SoftDeleteAsync(id);
                return OperationResult.Ok(OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> AddPaymentAsync(AddLoanPaymentDto dto)
        {
            try
            {
                if (dto.Amount <= 0)
                    return OperationResult.Fail("Payment amount must be greater than zero.");

                var loan = await _repo.GetByIdAsync(dto.LoanId);
                if (loan is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                if (loan.Status == LoanStatus.PaidOff)
                    return OperationResult.Fail("Cannot add payments to a paid-off loan.");

                var payment = new LoanPayment
                {
                    LoanId = dto.LoanId,
                    TransactionId = dto.TransactionId,
                    Amount = dto.Amount,
                    Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
                    Notes = dto.Notes?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddPaymentAsync(payment);
                return OperationResult.Ok(OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> UpdatePaymentAsync(UpdateLoanPaymentDto dto)
        {
            try
            {
                if (dto.Amount <= 0)
                    return OperationResult.Fail("Payment amount must be greater than zero.");

                var payment = await _repo.GetPaymentByIdAsync(dto.Id);
                if (payment is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                payment.TransactionId = dto.TransactionId;
                payment.Amount = dto.Amount;
                payment.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
                payment.Notes = dto.Notes?.Trim();

                await _repo.UpdatePaymentAsync(payment);
                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> DeletePaymentAsync(int paymentId)
        {
            try
            {
                var payment = await _repo.GetPaymentByIdAsync(paymentId);
                if (payment is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                await _repo.DeletePaymentAsync(paymentId);
                return OperationResult.Ok(OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> MarkPaidOffAsync(int id)
        {
            try
            {
                var entity = await _repo.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                entity.Status = LoanStatus.PaidOff;
                entity.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(entity);
                return OperationResult.Ok("Loan marked as paid off.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> RegenerateInstallmentsAsync(int loanId)
        {
            try
            {
                var loan = await _repo.GetByIdAsync(loanId);
                if (loan is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                if (loan.NumberOfInstallments is not > 0 || loan.FirstPaymentDate is null)
                    return OperationResult.Fail("Loan has no installment plan configured.");

                await _repo.DeleteInstallmentsByLoanAsync(loanId);
                var installments = LoanMapper.GenerateInstallments(loan);
                if (installments.Any())
                    await _repo.AddInstallmentsAsync(installments);

                return OperationResult.Ok("Installments regenerated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> UpdateInstallmentAsync(LoanInstallmentDto dto)
        {
            try
            {
                if (dto.ExpectedAmount <= 0)
                    return OperationResult.Fail("Expected amount must be greater than zero.");

                var entity = await _repo.GetInstallmentByIdAsync(dto.Id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                entity.ExpectedAmount = dto.ExpectedAmount;
                entity.DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc);
                entity.Notes = dto.Notes?.Trim();
                entity.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateInstallmentAsync(entity);
                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
