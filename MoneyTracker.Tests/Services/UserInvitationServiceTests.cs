using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class UserInvitationServiceTests
    {
        private readonly Mock<IUserInvitationRepository> _repository = new();
        private readonly Mock<ICurrentUserService> _currentUserService = new();
        private readonly Mock<IEmailSenderService> _emailSenderService = new();
        private readonly Mock<IErrorLogService> _errorLogService = new();
        private readonly Mock<IValidator<CreateUserInvitationDto>> _validator = new();
        private readonly Mock<ILogger<UserInvitationService>> _logger = new();

        private UserInvitationService CreateService()
            => new(_repository.Object, _currentUserService.Object, _emailSenderService.Object, _errorLogService.Object, _validator.Object, _logger.Object);

        [Fact]
        public async Task CreateOrResendAsync_WhenValidationFails_ReturnsFirstValidationError()
        {
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserInvitationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Email", "Email is required.") }));
            var service = CreateService();

            var result = await service.CreateOrResendAsync(new CreateUserInvitationDto());

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Email is required.");
        }

        [Fact]
        public async Task CreateOrResendAsync_WhenUserIsNotAuthenticated_ReturnsFail()
        {
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserInvitationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _currentUserService.SetupGet(x => x.IsAuthenticated).Returns(false);
            var service = CreateService();

            var result = await service.CreateOrResendAsync(new CreateUserInvitationDto
            {
                Email = "beta@example.com",
                InviteUrlTemplate = "https://localhost/invite/register?token={token}"
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Be("You must be signed in to send invitations.");
        }

        [Fact]
        public async Task CreateOrResendAsync_WhenEmailSendSucceeds_ReturnsInviteUrl()
        {
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserInvitationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _currentUserService.SetupGet(x => x.IsAuthenticated).Returns(true);
            _currentUserService.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
            _repository.Setup(x => x.GetByNormalizedEmailAsync("beta@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserInvitation?)null);
            _emailSenderService.Setup(x => x.SendAsync("beta@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult.Ok("sent"));
            var service = CreateService();

            var result = await service.CreateOrResendAsync(new CreateUserInvitationDto
            {
                Email = "beta@example.com",
                InviteUrlTemplate = "https://localhost/invite/register?token={token}"
            });

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Email.Should().Be("beta@example.com");
            result.Data.EmailSent.Should().BeTrue();
            result.Data.InviteUrl.Should().StartWith("https://localhost/invite/register?token=");
            _repository.Verify(x => x.AddAsync(It.Is<UserInvitation>(i => i.NormalizedEmail == "beta@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateOrResendAsync_WhenEmailSendFails_ReturnsManualShareMessage()
        {
            _validator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserInvitationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _currentUserService.SetupGet(x => x.IsAuthenticated).Returns(true);
            _currentUserService.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
            _repository.Setup(x => x.GetByNormalizedEmailAsync("beta@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserInvitation?)null);
            _emailSenderService.Setup(x => x.SendAsync("beta@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult.Fail("smtp missing"));
            var service = CreateService();

            var result = await service.CreateOrResendAsync(new CreateUserInvitationDto
            {
                Email = "beta@example.com",
                InviteUrlTemplate = "https://localhost/invite/register?token={token}"
            });

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.EmailSent.Should().BeFalse();
            result.Message.Should().Contain("Share the invite link manually");
        }

        [Fact]
        public async Task GetValidInvitationAsync_WhenInvitationExpired_ReturnsFail()
        {
            _repository.Setup(x => x.GetPendingByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInvitation
                {
                    Id = 7,
                    Email = "beta@example.com",
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
                });
            var service = CreateService();

            var result = await service.GetValidInvitationAsync("token");

            result.Success.Should().BeFalse();
            result.Message.Should().Be("This invitation is invalid or expired.");
        }

        [Fact]
        public async Task MarkAcceptedAsync_WhenInvitationExists_UpdatesInvitation()
        {
            var invitation = new UserInvitation { Id = 5, Email = "beta@example.com" };
            _repository.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
            var service = CreateService();

            var result = await service.MarkAcceptedAsync(5);

            result.Success.Should().BeTrue();
            invitation.AcceptedAtUtc.Should().NotBeNull();
            _repository.Verify(x => x.UpdateAsync(invitation, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
