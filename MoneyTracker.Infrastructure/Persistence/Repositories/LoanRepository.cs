using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly MoneyTrackerDbContext _context;
        private readonly ILogger<LoanRepository> _logger;

        public LoanRepository(MoneyTrackerDbContext context, ILogger<LoanRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Loan>> GetAllAsync()
        {
            try
            {
                return await _context.Loans
                    .AsNoTracking()
                    .Include(l => l.Payments)
                        .ThenInclude(p => p.Transaction)
                            .ThenInclude(t => t!.Account)
                    .Include(l => l.Installments)
                    .Where(l => !l.IsDeleted)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all loans.");
                return new();
            }
        }

        public async Task<Loan?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Loans
                    .Include(l => l.Payments)
                        .ThenInclude(p => p.Transaction)
                            .ThenInclude(t => t!.Account)
                    .Include(l => l.Installments)
                    .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting loan by ID {Id}.", id);
                return null;
            }
        }

        public async Task AddAsync(Loan loan)
        {
            try
            {
                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding loan.");
                throw;
            }
        }

        public async Task UpdateAsync(Loan loan)
        {
            try
            {
                _context.Loans.Update(loan);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating loan.");
                throw;
            }
        }

        public async Task SoftDeleteAsync(int id)
        {
            try
            {
                var entity = await _context.Loans.FindAsync(id);
                if (entity is null) return;

                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting loan with ID {Id}.", id);
                throw;
            }
        }

        public async Task AddPaymentAsync(LoanPayment payment)
        {
            try
            {
                _context.LoanPayments.Add(payment);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding loan payment.");
                throw;
            }
        }

        public async Task<LoanPayment?> GetPaymentByIdAsync(int paymentId)
        {
            try
            {
                return await _context.LoanPayments.FindAsync(paymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting loan payment by ID {Id}.", paymentId);
                return null;
            }
        }

        public async Task UpdatePaymentAsync(LoanPayment payment)
        {
            try
            {
                _context.LoanPayments.Update(payment);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating loan payment with ID {Id}.", payment.Id);
                throw;
            }
        }

        public async Task DeletePaymentAsync(int paymentId)
        {
            try
            {
                var entity = await _context.LoanPayments.FindAsync(paymentId);
                if (entity is null) return;

                _context.LoanPayments.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting loan payment with ID {Id}.", paymentId);
                throw;
            }
        }

        public async Task AddInstallmentsAsync(IEnumerable<LoanInstallment> installments)
        {
            try
            {
                _context.LoanInstallments.AddRange(installments);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding loan installments.");
                throw;
            }
        }

        public async Task UpdateInstallmentAsync(LoanInstallment installment)
        {
            try
            {
                _context.LoanInstallments.Update(installment);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating loan installment.");
                throw;
            }
        }

        public async Task DeleteInstallmentsByLoanAsync(int loanId)
        {
            try
            {
                var existing = await _context.LoanInstallments
                    .Where(i => i.LoanId == loanId)
                    .ToListAsync();
                _context.LoanInstallments.RemoveRange(existing);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting installments for loan {LoanId}.", loanId);
                throw;
            }
        }

        public async Task<LoanInstallment?> GetInstallmentByIdAsync(int installmentId)
        {
            try
            {
                return await _context.LoanInstallments.FindAsync(installmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting installment by ID {Id}.", installmentId);
                return null;
            }
        }
    }
}
