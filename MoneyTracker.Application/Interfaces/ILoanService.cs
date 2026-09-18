using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Loans;

namespace MoneyTracker.Application.Interfaces
{
    public interface ILoanService
    {
        Task<OperationResult<List<LoanDto>>> GetAllAsync();
        Task<OperationResult<LoanDto>> GetByIdAsync(int id);
        Task<OperationResult> CreateAsync(CreateLoanDto dto);
        Task<OperationResult> UpdateAsync(LoanDto dto);
        Task<OperationResult> DeleteAsync(int id);
        Task<OperationResult> AddPaymentAsync(AddLoanPaymentDto dto);
        Task<OperationResult> UpdatePaymentAsync(UpdateLoanPaymentDto dto);
        Task<OperationResult> DeletePaymentAsync(int paymentId);
        Task<OperationResult> MarkPaidOffAsync(int id);
        Task<OperationResult> RegenerateInstallmentsAsync(int loanId);
        Task<OperationResult> UpdateInstallmentAsync(LoanInstallmentDto dto);
    }
}
