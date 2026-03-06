namespace MoneyTracker.Application.Interfaces
{
    public interface IEmailSenderService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
