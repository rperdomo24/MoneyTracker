using FluentAssertions;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Merchants;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class MerchantServiceTests
    {
        private readonly Mock<IMerchantRepository> _repo = new();
        private readonly Mock<ITimeZoneService> _timeZoneService = new();
        private readonly Mock<ILogger<MerchantService>> _logger = new();

        private MerchantService CreateService() => new(_repo.Object, _timeZoneService.Object, _logger.Object);

        [Fact]
        public async Task GetAllAsync_MapsIsHiddenToDto()
        {
            _repo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Merchant>
            {
                new() { Id = 1, Name = "Shell", IsHidden = true },
                new() { Id = 2, Name = "Walmart" }
            });
            _repo.Setup(x => x.GetTransactionCountsAsync()).ReturnsAsync(new Dictionary<int, int>());
            var service = CreateService();

            var result = await service.GetAllAsync();

            result.Success.Should().BeTrue();
            result.Data!.Single(m => m.Id == 1).IsHidden.Should().BeTrue();
            result.Data!.Single(m => m.Id == 2).IsHidden.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_PersistsNameAndIsHidden()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Merchant { Id = 1, Name = "Old" });
            _repo.Setup(x => x.ExistsAsync("New", 1)).ReturnsAsync(false);
            _timeZoneService.Setup(x => x.GetNowInUtc()).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var service = CreateService();

            var result = await service.UpdateAsync(new MerchantDto { Id = 1, Name = " New ", IsHidden = true });

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Updated);
            _repo.Verify(x => x.UpdateAsync(It.Is<Merchant>(m => m.Id == 1 && m.Name == "New" && m.IsHidden)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_TrimsNameBeforeSaving()
        {
            _repo.Setup(x => x.ExistsAsync("Shell", null)).ReturnsAsync(false);
            _timeZoneService.Setup(x => x.GetNowInUtc()).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var service = CreateService();

            var result = await service.CreateAsync(new MerchantDto { Name = "  Shell " });

            result.Success.Should().BeTrue();
            _repo.Verify(x => x.AddAsync(It.Is<Merchant>(m => m.Name == "Shell")), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenNameIsBlank_ReturnsFailWithoutHittingRepository()
        {
            var service = CreateService();

            var result = await service.CreateAsync(new MerchantDto { Name = "   " });

            result.Success.Should().BeFalse();
            _repo.Verify(x => x.AddAsync(It.IsAny<Merchant>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenNameIsEmpty_ReturnsFailWithoutHittingRepository()
        {
            var service = CreateService();

            var result = await service.UpdateAsync(new MerchantDto { Id = 1, Name = "   " });

            result.Success.Should().BeFalse();
            _repo.Verify(x => x.UpdateAsync(It.IsAny<Merchant>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ReturnsFailResult()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Merchant?)null);
            var service = CreateService();

            var result = await service.UpdateAsync(new MerchantDto { Id = 1, Name = "Shell" });

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
            _repo.Verify(x => x.UpdateAsync(It.IsAny<Merchant>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenNameAlreadyExists_ReturnsFailResult()
        {
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Merchant { Id = 1, Name = "Old" });
            _repo.Setup(x => x.ExistsAsync("Shell", 1)).ReturnsAsync(true);
            var service = CreateService();

            var result = await service.UpdateAsync(new MerchantDto { Id = 1, Name = "Shell", IsHidden = true });

            result.Success.Should().BeFalse();
            _repo.Verify(x => x.UpdateAsync(It.IsAny<Merchant>()), Times.Never);
        }

        [Fact]
        public async Task SetHiddenAsync_WhenMerchantExists_UpdatesFlagAndPersists()
        {
            var merchant = new Merchant { Id = 1, Name = "Shell", IsHidden = false };
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(merchant);
            var service = CreateService();

            var result = await service.SetHiddenAsync(1, true);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.MerchantHidden);
            merchant.IsHidden.Should().BeTrue();
            _repo.Verify(x => x.UpdateAsync(merchant), Times.Once);
        }

        [Fact]
        public async Task SetHiddenAsync_WhenMerchantNotFound_Fails()
        {
            _repo.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Merchant?)null);
            var service = CreateService();

            var result = await service.SetHiddenAsync(99, true);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
            _repo.Verify(x => x.UpdateAsync(It.IsAny<Merchant>()), Times.Never);
        }    }
}
