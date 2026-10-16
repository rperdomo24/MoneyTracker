using FluentAssertions;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class SavingsGoalServiceTests
    {
        private static readonly DateTime Today = new(2026, 10, 16);

        private readonly Mock<ISavingsGoalRepository> _repo = new();
        private readonly Mock<ITimeZoneService> _timeZone = new();
        private readonly Mock<ILogger<SavingsGoalService>> _logger = new();

        public SavingsGoalServiceTests()
        {
            _timeZone.Setup(t => t.GetLocalTimeInConfiguredTimeZone()).Returns(Today.AddHours(9));
        }

        private SavingsGoalService CreateService() => new(_repo.Object, _timeZone.Object, _logger.Object);

        private static SavingsGoal BuildGoal(
            decimal target,
            decimal contributed,
            DateTime? targetDate,
            bool isCompleted = false) => new()
        {
            Id = 1,
            Name = "Vacation",
            TargetAmount = target,
            TargetDate = targetDate,
            IsCompleted = isCompleted,
            Contributions = contributed > 0
                ? new List<SavingsContribution>
                {
                    new() { Id = 1, GoalId = 1, Amount = contributed, Date = Today }
                }
                : new List<SavingsContribution>()
        };

        // ── GetAllAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ComputesRemainingDaysAndMonthlyPace()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SavingsGoal>
            {
                BuildGoal(target: 1000m, contributed: 400m, targetDate: Today.AddDays(60))
            });

            var result = await CreateService().GetAllAsync();

            result.Success.Should().BeTrue();
            var dto = result.Data!.Single();
            dto.AmountRemaining.Should().Be(600m);
            dto.DaysRemaining.Should().Be(60);
            dto.IsOverdue.Should().BeFalse();
            dto.MonthlyPaceNeeded.Should().Be(300m);
        }

        [Theory]
        [InlineData(15)]
        [InlineData(30)]
        public async Task GetAllAsync_WithAMonthOrLessLeft_HasNoMonthlyPace(int daysLeft)
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SavingsGoal>
            {
                BuildGoal(target: 10m, contributed: 0m, targetDate: Today.AddDays(daysLeft))
            });

            var result = await CreateService().GetAllAsync();

            var dto = result.Data!.Single();
            dto.DaysRemaining.Should().Be(daysLeft);
            dto.AmountRemaining.Should().Be(10m);
            dto.MonthlyPaceNeeded.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_JustOverAMonthLeft_PaceNeverExceedsRemaining()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SavingsGoal>
            {
                BuildGoal(target: 10m, contributed: 0m, targetDate: Today.AddDays(31))
            });

            var result = await CreateService().GetAllAsync();

            var dto = result.Data!.Single();
            dto.MonthlyPaceNeeded.Should().NotBeNull();
            dto.MonthlyPaceNeeded!.Value.Should().BeLessThanOrEqualTo(dto.AmountRemaining);
        }

        [Fact]
        public async Task GetAllAsync_WhenTargetDatePassedAndNotFunded_MarksOverdue()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SavingsGoal>
            {
                BuildGoal(target: 1000m, contributed: 400m, targetDate: Today.AddDays(-3))
            });

            var result = await CreateService().GetAllAsync();

            var dto = result.Data!.Single();
            dto.IsOverdue.Should().BeTrue();
            dto.DaysRemaining.Should().Be(-3);
            dto.MonthlyPaceNeeded.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_WhenCompleted_IsNeverOverdueAndHasNoPace()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SavingsGoal>
            {
                BuildGoal(target: 1000m, contributed: 400m, targetDate: Today.AddDays(-3), isCompleted: true)
            });

            var result = await CreateService().GetAllAsync();

            var dto = result.Data!.Single();
            dto.IsOverdue.Should().BeFalse();
            dto.MonthlyPaceNeeded.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_WhenOverfunded_RemainingIsZero()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SavingsGoal>
            {
                BuildGoal(target: 500m, contributed: 700m, targetDate: Today.AddDays(-1))
            });

            var result = await CreateService().GetAllAsync();

            var dto = result.Data!.Single();
            dto.AmountRemaining.Should().Be(0m);
            dto.IsOverdue.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_WithoutTargetDate_HasNoDaysOrPace()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SavingsGoal>
            {
                BuildGoal(target: 1000m, contributed: 100m, targetDate: null)
            });

            var result = await CreateService().GetAllAsync();

            var dto = result.Data!.Single();
            dto.DaysRemaining.Should().BeNull();
            dto.MonthlyPaceNeeded.Should().BeNull();
            dto.IsOverdue.Should().BeFalse();
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
        public async Task GetByIdAsync_WhenFound_ReturnsMappedDto()
        {
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
                BuildGoal(target: 1000m, contributed: 250m, targetDate: Today.AddDays(30)));

            var result = await CreateService().GetByIdAsync(1);

            result.Success.Should().BeTrue();
            result.Data!.AmountRemaining.Should().Be(750m);
            result.Data.DaysRemaining.Should().Be(30);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsFailResult()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SavingsGoal?)null);

            var result = await CreateService().GetByIdAsync(99);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        // ── ToggleCompletedAsync ───────────────────────────────────────────────

        [Fact]
        public async Task ToggleCompletedAsync_FlipsCompletedFlag()
        {
            var goal = BuildGoal(target: 1000m, contributed: 0m, targetDate: null);
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(goal);

            var result = await CreateService().ToggleCompletedAsync(1);

            result.Success.Should().BeTrue();
            goal.IsCompleted.Should().BeTrue();
            _repo.Verify(r => r.UpdateAsync(goal), Times.Once);
        }

        [Fact]
        public async Task ToggleCompletedAsync_WhenNotFound_ReturnsFailResult()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SavingsGoal?)null);

            var result = await CreateService().ToggleCompletedAsync(99);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<SavingsGoal>()), Times.Never);
        }
    }
}
