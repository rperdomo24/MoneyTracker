using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host)
                || string.IsNullOrWhiteSpace(_settings.SenderEmail)
                || string.IsNullOrWhiteSpace(_settings.Username)
                || string.IsNullOrWhiteSpace(_settings.Password))
            {
                _logger.LogWarning("SMTP not configured. Email to {ToEmail}. Subject: {Subject}. Body: {Body}", toEmail, subject, htmlBody);
                return;
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

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
