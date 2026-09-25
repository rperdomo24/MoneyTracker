using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _repository = new();
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _service = new NotificationService(_repository.Object, NullLogger<NotificationService>.Instance);
        }

        [Fact]
        public async Task DismissAsync_WhenRepositorySucceeds_ReturnsOk()
        {
            _repository.Setup(r => r.DismissAsync(5)).Returns(Task.CompletedTask);

            var result = await _service.DismissAsync(5);

            result.Success.Should().BeTrue();
            _repository.Verify(r => r.DismissAsync(5), Times.Once);
        }

        [Fact]
        public async Task DismissAsync_WhenRepositoryThrows_ReturnsFail()
        {
            _repository.Setup(r => r.DismissAsync(It.IsAny<int>())).ThrowsAsync(new Exception("boom"));

            var result = await _service.DismissAsync(5);

            result.Success.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DismissReadAsync_WhenRepositorySucceeds_ReturnsOk()
        {
            _repository.Setup(r => r.DismissAllReadAsync()).Returns(Task.CompletedTask);

            var result = await _service.DismissReadAsync();

            result.Success.Should().BeTrue();
            _repository.Verify(r => r.DismissAllReadAsync(), Times.Once);
        }

        [Fact]
        public async Task DismissReadAsync_WhenRepositoryThrows_ReturnsFail()
        {
            _repository.Setup(r => r.DismissAllReadAsync()).ThrowsAsync(new Exception("boom"));

            var result = await _service.DismissReadAsync();

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task MarkAllReadAsync_WhenRepositorySucceeds_ReturnsOk()
        {
            _repository.Setup(r => r.MarkAllReadAsync()).Returns(Task.CompletedTask);

            var result = await _service.MarkAllReadAsync();

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task MarkAllReadAsync_WhenRepositoryThrows_ReturnsFail()
        {
            _repository.Setup(r => r.MarkAllReadAsync()).ThrowsAsync(new Exception("boom"));

            var result = await _service.MarkAllReadAsync();

            result.Success.Should().BeFalse();
        }
    }
}
