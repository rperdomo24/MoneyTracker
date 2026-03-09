using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.Interfaces;

namespace MoneyTracker.Infrastructure.Services
{
    public class SmtpEmailSenderService : IEmailSenderService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSenderService> _logger;

        public SmtpEmailSenderService(IOptions<EmailSettings> options, ILogger<SmtpEmailSenderService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public OperationResult ValidateConfiguration()
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(_settings.Host)
                || _settings.Port <= 0
                || string.IsNullOrWhiteSpace(_settings.SenderEmail)
                || string.IsNullOrWhiteSpace(_settings.Username)
                || string.IsNullOrWhiteSpace(_settings.Password))
            {
                if (string.IsNullOrWhiteSpace(_settings.Host))
                {
                    missingFields.Add(nameof(EmailSettings.Host));
                }

                if (_settings.Port <= 0)
                {
                    missingFields.Add(nameof(EmailSettings.Port));
                }

                if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
                {
                    missingFields.Add(nameof(EmailSettings.SenderEmail));
                }

                if (string.IsNullOrWhiteSpace(_settings.Username))
                {
                    missingFields.Add(nameof(EmailSettings.Username));
                }

                if (string.IsNullOrWhiteSpace(_settings.Password))
                {
                    missingFields.Add(nameof(EmailSettings.Password));
                }

                return OperationResult.Fail($"SMTP not configured. Missing or invalid fields: {string.Join(", ", missingFields)}.");
            }

            return OperationResult.Ok("SMTP settings are configured.");
        }

        public async Task<OperationResult> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var validation = ValidateConfiguration();
            if (!validation.Success)
            {
                _logger.LogWarning("SMTP validation failed before sending email to {ToEmail}. Reason: {Reason}", toEmail, validation.Message);
                return validation;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            try
            {
                await client.SendMailAsync(message, cancellationToken);
                return OperationResult.Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}. Subject: {Subject}", toEmail, subject);
                return OperationResult.Fail("Unable to send email with current SMTP settings.");
            }
        }
    }
}
