using MoneyTracker.Application.Common;

namespace MoneyTracker.Application.Interfaces
{
    public interface IEmailSenderService
    {
        OperationResult ValidateConfiguration();
        Task<OperationResult> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
