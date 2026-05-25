using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Domain.Interfaces
{
    public interface ILoanRepository
    {
        Task<List<Loan>> GetAllAsync();
        Task<Loan?> GetByIdAsync(int id);
        Task AddAsync(Loan loan);
        Task UpdateAsync(Loan loan);
        Task SoftDeleteAsync(int id);
        Task AddPaymentAsync(LoanPayment payment);
        Task<LoanPayment?> GetPaymentByIdAsync(int paymentId);
        Task UpdatePaymentAsync(LoanPayment payment);
        Task DeletePaymentAsync(int paymentId);
        Task AddInstallmentsAsync(IEnumerable<LoanInstallment> installments);
        Task UpdateInstallmentAsync(LoanInstallment installment);
        Task DeleteInstallmentsByLoanAsync(int loanId);
        Task<LoanInstallment?> GetInstallmentByIdAsync(int installmentId);
    }
}
