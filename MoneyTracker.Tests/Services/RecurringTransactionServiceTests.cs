using FluentAssertions;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.RecurringTransactions;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class RecurringTransactionServiceTests
    {
        private readonly Mock<IRecurringTransactionRepository> _repo = new();
        private readonly Mock<ILogger<RecurringTransactionService>> _logger = new();

        private RecurringTransactionService CreateService() => new(_repo.Object, _logger.Object);

        private static RecurringTransaction BuildEntity(RecurringStatus status) => new()
        {
            Id = 1,
            Name = "Netflix",
            Amount = 15m,
            CategoryId = 1,
            AccountId = 1,
            Frequency = RecurringFrequency.Monthly,
            Status = status
        };

        // ── SetStatusAsync ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(RecurringStatus.Active, RecurringStatus.Paused, "Paused.")]
        [InlineData(RecurringStatus.Paused, RecurringStatus.Active, "Resumed.")]
        [InlineData(RecurringStatus.Active, RecurringStatus.Completed, "Marked as completed.")]
        [InlineData(RecurringStatus.Paused, RecurringStatus.Completed, "Marked as completed.")]
        [InlineData(RecurringStatus.Completed, RecurringStatus.Active, "Reopened.")]
        public async Task SetStatusAsync_AllowedTransition_UpdatesStatus(
            RecurringStatus from, RecurringStatus to, string expectedMessage)
        {
            var entity = BuildEntity(from);
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

            var result = await CreateService().SetStatusAsync(1, to);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(expectedMessage);
            entity.Status.Should().Be(to);
            _repo.Verify(r => r.UpdateAsync(entity), Times.Once);
        }

        [Theory]
        [InlineData(RecurringStatus.Completed, RecurringStatus.Paused)]
        [InlineData(RecurringStatus.Active, RecurringStatus.Active)]
        [InlineData(RecurringStatus.Paused, RecurringStatus.Paused)]
        [InlineData(RecurringStatus.Completed, RecurringStatus.Completed)]
        public async Task SetStatusAsync_InvalidTransition_FailsWithoutSaving(
            RecurringStatus from, RecurringStatus to)
        {
            var entity = BuildEntity(from);
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

            var result = await CreateService().SetStatusAsync(1, to);

            result.Success.Should().BeFalse();
            entity.Status.Should().Be(from);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<RecurringTransaction>()), Times.Never);
        }

        [Fact]
        public async Task SetStatusAsync_WhenNotFound_Fails()
        {
            _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RecurringTransaction?)null);

            var result = await CreateService().SetStatusAsync(99, RecurringStatus.Paused);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<RecurringTransaction>()), Times.Never);
        }

        [Fact]
        public async Task SetStatusAsync_WhenRepositoryThrows_ReturnsSafeMessage()
        {
            _repo.Setup(r => r.GetByIdAsync(1)).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().SetStatusAsync(1, RecurringStatus.Paused);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.UnexpectedError);
        }

        // ── UpdateAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_DoesNotChangeExistingStatus()
        {
            var entity = BuildEntity(RecurringStatus.Paused);
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

            var dto = new RecurringTransactionDto
            {
                Id = 1,
                Name = "Netflix Premium",
                Amount = 20m,
                CategoryId = 1,
                AccountId = 1,
                Frequency = RecurringFrequency.Monthly,
                Status = RecurringStatus.Active
            };

            var result = await CreateService().UpdateAsync(dto);

            result.Success.Should().BeTrue();
            entity.Name.Should().Be("Netflix Premium");
            entity.Amount.Should().Be(20m);
            entity.Status.Should().Be(RecurringStatus.Paused);
            _repo.Verify(r => r.UpdateAsync(entity), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_Fails()
        {
            _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RecurringTransaction?)null);

            var result = await CreateService().UpdateAsync(new RecurringTransactionDto
            {
                Id = 99,
                Name = "Ghost",
                Amount = 5m
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }
    }
}
