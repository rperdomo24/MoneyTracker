using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Jobs
{
    public class CardReminderJob
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailSenderService _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CardReminderJob> _logger;

        public CardReminderJob(
            IAccountRepository accountRepository,
            IEmailSenderService emailSender,
            UserManager<ApplicationUser> userManager,
            ILogger<CardReminderJob> logger)
        {
            _accountRepository = accountRepository;
            _emailSender = emailSender;
            _userManager = userManager;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task ExecuteAsync()
        {
            var today = DateTime.Today;
            var accounts = await _accountRepository.GetAllCreditAccountsWithDatesAsync();

            foreach (var account in accounts)
            {
                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.TenantId == account.TenantId);

                if (user?.Email is null)
                    continue;

                if (account.CutDay.HasValue)
                {
                    var cutDay = account.CutDay.Value;
                    if (today.Day == cutDay || today.Day == cutDay - 2)
                    {
                        var daysUntil = cutDay - today.Day;
                        var subject = daysUntil == 0
                            ? $"Hoy es tu fecha de corte — {account.CardDisplayName ?? account.Name}"
                            : $"Tu fecha de corte es en {daysUntil} días — {account.CardDisplayName ?? account.Name}";

                        var html = BuildCutDayEmail(account.CardDisplayName ?? account.Name, account.BankName, cutDay, daysUntil);
                        await SendWithLogging(user.Email, subject, html);
                    }
                }

                if (account.PaymentDay.HasValue)
                {
                    var payDay = account.PaymentDay.Value;
                    if (today.Day == payDay || today.Day == payDay - 2)
                    {
                        var daysUntil = payDay - today.Day;
                        var subject = daysUntil == 0
                            ? $"Hoy es tu fecha de pago — {account.CardDisplayName ?? account.Name}"
                            : $"Tu fecha de pago es en {daysUntil} días — {account.CardDisplayName ?? account.Name}";

                        var html = BuildPaymentDayEmail(account.CardDisplayName ?? account.Name, account.BankName, payDay, daysUntil);
                        await SendWithLogging(user.Email, subject, html);
                    }
                }
            }
        }

        private async Task SendWithLogging(string email, string subject, string html)
        {
            var result = await _emailSender.SendAsync(email, subject, html);
            if (!result.Success)
                _logger.LogWarning("Card reminder email failed. To: {Email}. Reason: {Reason}", email, result.Message);
        }

        private static string BuildCutDayEmail(string cardName, string? bankName, int cutDay, int daysUntil)
        {
            var heading = daysUntil == 0 ? "Hoy es tu fecha de corte" : $"Tu fecha de corte es en {daysUntil} día(s)";
            var detail = daysUntil == 0
                ? "Hoy se cierra el periodo de tu tarjeta. Revisa tus gastos del mes."
                : $"El día {cutDay} de este mes se cierra el periodo de facturación de tu tarjeta.";

            return BuildBaseEmail(cardName, bankName, heading, detail, "#1976D2", "Fecha de Corte");
        }

        private static string BuildPaymentDayEmail(string cardName, string? bankName, int payDay, int daysUntil)
        {
            var heading = daysUntil == 0 ? "Hoy es tu fecha de pago" : $"Tu fecha de pago es en {daysUntil} día(s)";
            var detail = daysUntil == 0
                ? "Hoy vence tu pago. Evita cargos por mora realizando tu pago a tiempo."
                : $"El día {payDay} de este mes vence el pago de tu tarjeta.";

            return BuildBaseEmail(cardName, bankName, heading, detail, "#E53935", "Fecha de Pago");
        }

        private static string BuildBaseEmail(string cardName, string? bankName, string heading, string detail, string accentColor, string badge)
        {
            var bank = string.IsNullOrWhiteSpace(bankName) ? string.Empty : $"<p style='margin:0;color:#666;font-size:14px;'>{bankName}</p>";

            return $"""
                <!DOCTYPE html>
                <html lang="es">
                <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f5;padding:32px 0;">
                    <tr><td align="center">
                      <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
                        <!-- Header -->
                        <tr>
                          <td style="background:{accentColor};padding:28px 32px;">
                            <p style="margin:0;color:rgba(255,255,255,0.8);font-size:12px;text-transform:uppercase;letter-spacing:1px;">MoneyTracker</p>
                            <h1 style="margin:8px 0 0;color:#ffffff;font-size:22px;">{heading}</h1>
                          </td>
                        </tr>
                        <!-- Body -->
                        <tr>
                          <td style="padding:32px;">
                            <table width="100%" cellpadding="0" cellspacing="0">
                              <tr>
                                <td style="background:#f8f9fa;border-left:4px solid {accentColor};border-radius:4px;padding:16px 20px;">
                                  <p style="margin:0;font-size:13px;color:{accentColor};font-weight:bold;text-transform:uppercase;">{badge}</p>
                                  <p style="margin:6px 0 0;font-size:20px;font-weight:bold;color:#212121;">{cardName}</p>
                                  {bank}
                                </td>
                              </tr>
                            </table>
                            <p style="margin:24px 0 0;font-size:15px;color:#424242;line-height:1.6;">{detail}</p>
                            <p style="margin:12px 0 0;font-size:13px;color:#9e9e9e;">Este es un recordatorio automático de MoneyTracker.</p>
                          </td>
                        </tr>
                        <!-- Footer -->
                        <tr>
                          <td style="background:#fafafa;border-top:1px solid #eeeeee;padding:16px 32px;text-align:center;">
                            <p style="margin:0;font-size:12px;color:#bdbdbd;">MoneyTracker &copy; {DateTime.UtcNow.Year}</p>
                          </td>
                        </tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;
        }
    }
}
