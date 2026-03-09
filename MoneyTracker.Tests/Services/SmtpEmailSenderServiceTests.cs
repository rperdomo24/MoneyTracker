using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Infrastructure.Services;

namespace MoneyTracker.Tests.Services
{
    public class SmtpEmailSenderServiceTests
    {
        [Fact]
        public void ValidateConfiguration_WhenSettingsAreMissing_ReturnsFail()
        {
            var service = CreateService(new EmailSettings());

            var result = service.ValidateConfiguration();

            result.Success.Should().BeFalse();
            result.Message.Should().Contain(nameof(EmailSettings.Host));
            result.Message.Should().Contain(nameof(EmailSettings.SenderEmail));
            result.Message.Should().Contain(nameof(EmailSettings.Username));
            result.Message.Should().Contain(nameof(EmailSettings.Password));
        }

        [Fact]
        public void ValidateConfiguration_WhenSettingsAreComplete_ReturnsOk()
        {
            var service = CreateService(new EmailSettings
            {
                Host = "smtp.example.com",
                Port = 587,
                EnableSsl = true,
                SenderEmail = "noreply@example.com",
                SenderName = "MoneyTracker",
                Username = "smtp-user",
                Password = "smtp-password"
            });

            var result = service.ValidateConfiguration();

            result.Success.Should().BeTrue();
            result.Message.Should().Be("SMTP settings are configured.");
        }

        [Fact]
        public async Task SendAsync_WhenSettingsAreMissing_ReturnsFail()
        {
            var service = CreateService(new EmailSettings());

            var result = await service.SendAsync("user@example.com", "Subject", "<p>Hello</p>");

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("SMTP not configured.");
        }

        private static SmtpEmailSenderService CreateService(EmailSettings settings)
            => new(Options.Create(settings), NullLogger<SmtpEmailSenderService>.Instance);
    }
}
