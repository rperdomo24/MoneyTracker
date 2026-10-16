using FluentAssertions;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Loans;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Loans;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class LoanServiceTests
    {
        private readonly Mock<ILoanRepository> _repo = new();
        private readonly Mock<ILogger<LoanService>> _logger = new();

        private LoanService CreateService() => new(_repo.Object, _logger.Object);

        // ── GetAllAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsMappedLoans()
        {
            var loans = new List<Loan>
            {
                new Loan
                {
                    Id = 1,
                    ContactName = "John",
                    PrincipalAmount = 500m,
                    StartDate = DateTime.UtcNow,
                    Status = LoanStatus.Active,
                    Payments = new List<LoanPayment>()
                }
            };
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(loans);

            var result = await CreateService().GetAllAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data![0].ContactName.Should().Be("John");
            result.Data[0].Balance.Should().Be(500m);
        }

        [Fact]
        public async Task GetAllAsync_WhenRepositoryThrows_ReturnsFailResult()
        {
            _repo.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

            var result = await CreateService().GetAllAsync();

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.UnexpectedError);
        }

        // ── GetByIdAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsFailResult()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Loan?)null);

            var result = await CreateService().GetByIdAsync(99);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_ReturnsMappedDto()
        {
            var loan = new Loan
            {
                Id = 5,
                ContactName = "Maria",
                PrincipalAmount = 1000m,
                StartDate = DateTime.UtcNow,
                Status = LoanStatus.Active,
                Payments = new List<LoanPayment>
                {
                    new LoanPayment { Id = 1, LoanId = 5, Amount = 300m, Date = DateTime.UtcNow }
                }
            };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(loan);

            var result = await CreateService().GetByIdAsync(5);

            result.Success.Should().BeTrue();
            result.Data!.TotalPaid.Should().Be(300m);
            result.Data.Balance.Should().Be(700m);
            result.Data.ProgressPercent.Should().Be(30m);
        }

        // ── CreateAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_WithValidDto_CallsAddAndReturnsSuccess()
        {
            _repo.Setup(r => r.AddAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);

            var dto = new CreateLoanDto
            {
                ContactName = "Pedro",
                PrincipalAmount = 250m,
                StartDate = DateTime.UtcNow
            };

            var result = await CreateService().CreateAsync(dto);

            result.Success.Should().BeTrue();
            _repo.Verify(r => r.AddAsync(It.Is<Loan>(l => l.ContactName == "Pedro")), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithEmptyContactName_ReturnsFailResult()
        {
            var dto = new CreateLoanDto { ContactName = "  ", PrincipalAmount = 100m };

            var result = await CreateService().CreateAsync(dto);

            result.Success.Should().BeFalse();
            _repo.Verify(r => r.AddAsync(It.IsAny<Loan>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithZeroAmount_ReturnsFailResult()
        {
            var dto = new CreateLoanDto { ContactName = "Pedro", PrincipalAmount = 0m };

            var result = await CreateService().CreateAsync(dto);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_WithInstallments_SetsDueDateToLastInstallment()
        {
            var dto = new CreateLoanDto
            {
                ContactName = "Pedro",
                PrincipalAmount = 600m,
                StartDate = new DateTime(2026, 1, 1),
                NumberOfInstallments = 6,
                FirstPaymentDate = new DateTime(2026, 1, 15),
                PaymentFrequency = PaymentFrequency.Monthly,
                DueDate = new DateTime(2026, 12, 31)
            };

            var result = await CreateService().CreateAsync(dto);

            result.Success.Should().BeTrue();
            _repo.Verify(r => r.AddAsync(It.Is<Loan>(l => l.DueDate == new DateTime(2026, 6, 15))), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithoutInstallments_ClearsInstallmentFieldsAndKeepsDueDate()
        {
            var dto = new CreateLoanDto
            {
                ContactName = "Pedro",
                PrincipalAmount = 600m,
                StartDate = new DateTime(2026, 1, 1),
                FirstPaymentDate = new DateTime(2026, 1, 15),
                PaymentFrequency = PaymentFrequency.Weekly,
                DueDate = new DateTime(2026, 4, 1)
            };

            var result = await CreateService().CreateAsync(dto);

            result.Success.Should().BeTrue();
            _repo.Verify(r => r.AddAsync(It.Is<Loan>(l =>
                l.NumberOfInstallments == null &&
                l.FirstPaymentDate == null &&
                l.PaymentFrequency == null &&
                l.DueDate == new DateTime(2026, 4, 1))), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_SwitchingToInstallments_DerivesDueDateFromLastInstallment()
        {
            var loan = new Loan
            {
                Id = 20,
                ContactName = "Marta",
                PrincipalAmount = 400m,
                StartDate = new DateTime(2026, 1, 1),
                DueDate = new DateTime(2026, 3, 1),
                Status = LoanStatus.Active,
                Payments = new List<LoanPayment>()
            };
            _repo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(loan);

            var dto = new LoanDto
            {
                Id = 20,
                ContactName = "Marta",
                PrincipalAmount = 400m,
                StartDate = new DateTime(2026, 1, 1),
                NumberOfInstallments = 4,
                FirstPaymentDate = new DateTime(2026, 2, 1),
                PaymentFrequency = PaymentFrequency.Biweekly,
                Status = LoanStatus.Active
            };

            var result = await CreateService().UpdateAsync(dto);

            result.Success.Should().BeTrue();
            // 4 biweekly installments starting Feb 1 → last on Feb 1 + 42 days
            _repo.Verify(r => r.UpdateAsync(It.Is<Loan>(l => l.DueDate == new DateTime(2026, 3, 15))), Times.Once);
            _repo.Verify(r => r.AddInstallmentsAsync(It.Is<List<LoanInstallment>>(i => i.Count == 4)), Times.Once);
        }

        // ── AddPaymentAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task AddPaymentAsync_ValidPayment_AddsAndReturnsSuccess()
        {
            var loan = new Loan
            {
                Id = 1,
                ContactName = "Luis",
                PrincipalAmount = 400m,
                StartDate = DateTime.UtcNow,
                Status = LoanStatus.Active,
                Payments = new List<LoanPayment>()
            };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(loan);
            _repo.Setup(r => r.AddPaymentAsync(It.IsAny<LoanPayment>())).Returns(Task.CompletedTask);

            var dto = new AddLoanPaymentDto
            {
                LoanId = 1,
                Amount = 200m,
                Date = DateTime.UtcNow
            };

            var result = await CreateService().AddPaymentAsync(dto);

            result.Success.Should().BeTrue();
            _repo.Verify(r => r.AddPaymentAsync(It.Is<LoanPayment>(p =>
                p.LoanId == 1 && p.Amount == 200m)), Times.Once);
        }

        [Fact]
        public async Task AddPaymentAsync_WithZeroAmount_ReturnsFailResult()
        {
            var dto = new AddLoanPaymentDto { LoanId = 1, Amount = 0m, Date = DateTime.UtcNow };

            var result = await CreateService().AddPaymentAsync(dto);

            result.Success.Should().BeFalse();
            _repo.Verify(r => r.AddPaymentAsync(It.IsAny<LoanPayment>()), Times.Never);
        }

        [Fact]
        public async Task AddPaymentAsync_LoanNotFound_ReturnsFailResult()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Loan?)null);

            var dto = new AddLoanPaymentDto { LoanId = 999, Amount = 100m, Date = DateTime.UtcNow };

            var result = await CreateService().AddPaymentAsync(dto);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task AddPaymentAsync_ToClosedLoan_ReturnsFailResult()
        {
            var loan = new Loan
            {
                Id = 2,
                ContactName = "Ana",
                PrincipalAmount = 100m,
                Status = LoanStatus.PaidOff,
                Payments = new List<LoanPayment>()
            };
            _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(loan);

            var dto = new AddLoanPaymentDto { LoanId = 2, Amount = 50m, Date = DateTime.UtcNow };

            var result = await CreateService().AddPaymentAsync(dto);

            result.Success.Should().BeFalse();
        }

        // ── MarkPaidOffAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task MarkPaidOffAsync_WhenFound_UpdatesStatusAndReturnsSuccess()
        {
            var loan = new Loan
            {
                Id = 3,
                ContactName = "Carlos",
                PrincipalAmount = 200m,
                Status = LoanStatus.Active,
                Payments = new List<LoanPayment>()
            };
            _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(loan);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);

            var result = await CreateService().MarkPaidOffAsync(3);

            result.Success.Should().BeTrue();
            _repo.Verify(r => r.UpdateAsync(It.Is<Loan>(l => l.Status == LoanStatus.PaidOff)), Times.Once);
        }

        [Fact]
        public async Task MarkPaidOffAsync_WhenNotFound_ReturnsFailResult()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Loan?)null);

            var result = await CreateService().MarkPaidOffAsync(99);

            result.Success.Should().BeFalse();
        }

        // ── ForgiveAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task ForgiveAsync_WhenActive_SetsForgivenAndAppendsReason()
        {
            var loan = new Loan
            {
                Id = 4,
                ContactName = "Rosa",
                PrincipalAmount = 300m,
                Status = LoanStatus.Active,
                Notes = "Initial note",
                Payments = new List<LoanPayment>()
            };
            _repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(loan);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);

            var result = await CreateService().ForgiveAsync(4, "  Family help  ");

            result.Success.Should().BeTrue();
            _repo.Verify(r => r.UpdateAsync(It.Is<Loan>(l =>
                l.Status == LoanStatus.Forgiven &&
                l.Notes!.StartsWith("Initial note") &&
                l.Notes.Contains("Family help"))), Times.Once);
        }

        [Fact]
        public async Task ForgiveAsync_WhenNotFound_ReturnsFailResult()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Loan?)null);

            var result = await CreateService().ForgiveAsync(99, null);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task ForgiveAsync_WhenNotActive_ReturnsFailResult()
        {
            var loan = new Loan { Id = 5, ContactName = "Tom", Status = LoanStatus.PaidOff, Payments = new List<LoanPayment>() };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(loan);

            var result = await CreateService().ForgiveAsync(5, "reason");

            result.Success.Should().BeFalse();
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Never);
        }

        // ── GetPaymentByTransactionIdAsync ─────────────────────────────────────

        [Fact]
        public async Task GetPaymentByTransactionIdAsync_WhenLinked_ReturnsMappedPayment()
        {
            var payment = new LoanPayment
            {
                Id = 3,
                LoanId = 5,
                Loan = new Loan { Id = 5, ContactName = "Ana" },
                TransactionId = 40,
                Amount = 25m
            };
            _repo.Setup(r => r.GetPaymentByTransactionIdAsync(40)).ReturnsAsync(payment);

            var result = await CreateService().GetPaymentByTransactionIdAsync(40);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.LoanContactName.Should().Be("Ana");
            result.Data.Amount.Should().Be(25m);
        }

        [Fact]
        public async Task GetPaymentByTransactionIdAsync_WhenNotLinked_ReturnsNullData()
        {
            _repo.Setup(r => r.GetPaymentByTransactionIdAsync(41)).ReturnsAsync((LoanPayment?)null);

            var result = await CreateService().GetPaymentByTransactionIdAsync(41);

            result.Success.Should().BeTrue();
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetPaymentByTransactionIdAsync_WhenRepositoryThrows_ReturnsFailResult()
        {
            _repo.Setup(r => r.GetPaymentByTransactionIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("boom"));

            var result = await CreateService().GetPaymentByTransactionIdAsync(42);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.UnexpectedError);
        }

        // ── ReopenAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task ReopenAsync_WhenClosed_SetsActive()
        {
            var loan = new Loan { Id = 6, ContactName = "Eva", Status = LoanStatus.Forgiven, Payments = new List<LoanPayment>() };
            _repo.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(loan);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);

            var result = await CreateService().ReopenAsync(6);

            result.Success.Should().BeTrue();
            _repo.Verify(r => r.UpdateAsync(It.Is<Loan>(l => l.Status == LoanStatus.Active)), Times.Once);
        }

        [Fact]
        public async Task ReopenAsync_WhenAlreadyActive_ReturnsFailResult()
        {
            var loan = new Loan { Id = 7, ContactName = "Leo", Status = LoanStatus.Active, Payments = new List<LoanPayment>() };
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(loan);

            var result = await CreateService().ReopenAsync(7);

            result.Success.Should().BeFalse();
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Never);
        }

        [Fact]
        public async Task AddPaymentAsync_ToForgivenLoan_ReturnsFailResult()
        {
            var loan = new Loan { Id = 8, ContactName = "Ivan", PrincipalAmount = 100m, Status = LoanStatus.Forgiven, Payments = new List<LoanPayment>() };
            _repo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(loan);

            var result = await CreateService().AddPaymentAsync(new AddLoanPaymentDto { LoanId = 8, Amount = 10m, Date = DateTime.UtcNow });

            result.Success.Should().BeFalse();
            _repo.Verify(r => r.AddPaymentAsync(It.IsAny<LoanPayment>()), Times.Never);
        }

        // ── DeletePaymentAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task DeletePaymentAsync_WhenFound_DeletesAndReturnsSuccess()
        {
            var payment = new LoanPayment { Id = 10, LoanId = 1, Amount = 100m, Date = DateTime.UtcNow };
            _repo.Setup(r => r.GetPaymentByIdAsync(10)).ReturnsAsync(payment);
            _repo.Setup(r => r.DeletePaymentAsync(10)).Returns(Task.CompletedTask);

            var result = await CreateService().DeletePaymentAsync(10);

            result.Success.Should().BeTrue();
            _repo.Verify(r => r.DeletePaymentAsync(10), Times.Once);
        }

        [Fact]
        public async Task DeletePaymentAsync_WhenNotFound_ReturnsFailResult()
        {
            _repo.Setup(r => r.GetPaymentByIdAsync(It.IsAny<int>())).ReturnsAsync((LoanPayment?)null);

            var result = await CreateService().DeletePaymentAsync(99);

            result.Success.Should().BeFalse();
        }
    }
}
