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
