using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.CardBenefits;
using MoneyTracker.Application.Services;
using MoneyTracker.Application.Validators.CardBenefits;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.CardBenefits;
using MoneyTracker.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class CardBenefitServiceTests
    {
        private readonly Mock<ICardBenefitRepository> _repo = new();
        private readonly Mock<IValidator<CardBenefitDto>> _validator = new();

        private CardBenefitService CreateService()
            => new CardBenefitService(_repo.Object, _validator.Object, NullLogger<CardBenefitService>.Instance);

        [Fact]
        public async Task GetAllAsync_ReturnsAllBenefits()
        {
            _repo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<CardBenefit>
                {
                    new() { Id = 1, Name = "5% Cashback Groceries", BenefitType = BenefitType.Cashback, BenefitRate = 0.05m, AccountId = 1 },
                    new() { Id = 2, Name = "2x Points Gas", BenefitType = BenefitType.Points, BenefitRate = 0.02m, AccountId = 2 }
                });

            var result = await CreateService().GetAllAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsFail()
        {
            _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CardBenefit?)null);

            var result = await CreateService().GetByIdAsync(999);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_CallsRepositoryAndReturnsOk()
        {
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CardBenefitDto>(), default))
                .ReturnsAsync(new ValidationResult());

            var dto = new CardBenefitDto
            {
                AccountId = 1,
                Name = "5% Cashback",
                BenefitType = BenefitType.Cashback,
                BenefitRate = 0.05m,
                IsActive = true
            };

            var result = await CreateService().CreateAsync(dto);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Created);
            _repo.Verify(x => x.AddAsync(It.IsAny<CardBenefit>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_InvalidDto_ReturnsFail()
        {
            var validationResult = new ValidationResult(new List<ValidationFailure>
            {
                new("BenefitRate", "Benefit rate must be greater than 0.")
            });
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CardBenefitDto>(), default))
                .ReturnsAsync(validationResult);

            var result = await CreateService().CreateAsync(new CardBenefitDto());

            result.Success.Should().BeFalse();
            _repo.Verify(x => x.AddAsync(It.IsAny<CardBenefit>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_ReturnsFail()
        {
            _repo.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CardBenefit?)null);

            var result = await CreateService().DeleteAsync(999);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
            _repo.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenFound_ReturnsOk()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new CardBenefit { Id = 1, Name = "Rule" });

            var result = await CreateService().DeleteAsync(1);

            result.Success.Should().BeTrue();
            _repo.Verify(x => x.DeleteAsync(1), Times.Once);
        }
    }
}
