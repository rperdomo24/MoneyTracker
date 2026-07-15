using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Loans;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Jobs
{
    public class NotificationGeneratorJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly INotificationRepository _notificationRepo;
        private readonly IEmailSenderService _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationGeneratorJob> _logger;

        private readonly ITimeZoneService _timeZoneService;

        public NotificationGeneratorJob(
            IServiceScopeFactory scopeFactory,
            INotificationRepository notificationRepo,
            IEmailSenderService emailSender,
            UserManager<ApplicationUser> userManager,
            ITimeZoneService timeZoneService,
            ILogger<NotificationGeneratorJob> logger)
        {
            _scopeFactory = scopeFactory;
            _notificationRepo = notificationRepo;
            _emailSender = emailSender;
            _userManager = userManager;
            _timeZoneService = timeZoneService;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task ExecuteAsync()
        {
            var today = DateTime.SpecifyKind(
                _timeZoneService.GetLocalTimeInConfiguredTimeZone(),
                DateTimeKind.Utc);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<Persistence.MoneyTrackerDbContext>();

            await CheckBudgetAlertsAsync(context, today);
            await CheckLoanRemindersAsync(context, today);
            await CheckCreditCardAlertsAsync(context, today);
        }

        private async Task CheckBudgetAlertsAsync(Persistence.MoneyTrackerDbContext context, DateTime today)
        {
            try
            {
                var budgets = await context.Budgets
                    .IgnoreQueryFilters()
                    .Include(b => b.Category)
                    .Where(b => !b.IsDeleted && b.Year == today.Year && b.Month == today.Month)
                    .ToListAsync();

                if (budgets.Count == 0) return;

                var categoryIds = budgets.Select(b => b.CategoryId).ToHashSet();
                var tenantIds = budgets.Select(b => b.TenantId).ToHashSet();

                var transactions = await context.Transaction
                    .IgnoreQueryFilters()
                    .Where(t => !t.IsDeleted
                             && t.Date.Year == today.Year
                             && t.Date.Month == today.Month
                             && categoryIds.Contains(t.CategoryId)
                             && tenantIds.Contains(t.TenantId))
                    .ToListAsync();

                var spendingMap = transactions
                    .GroupBy(t => (t.TenantId, t.CategoryId))
                    .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount)));

                foreach (var budget in budgets)
                {
                    try
                    {
                        spendingMap.TryGetValue((budget.TenantId, budget.CategoryId), out var spending);
                        if (spending < budget.Amount) continue;

                        var key = $"budget-alert-{budget.Id}-{today.Year}-{today.Month:D2}";
                        if (await _notificationRepo.ExistsByDuplicateKeyTodayAsync(key)) continue;

                        var percent = budget.Amount > 0 ? (int)Math.Round(spending / budget.Amount * 100) : 100;
                        var categoryName = budget.Category?.Name ?? "Budget";

                        var notification = new AppNotification
                        {
                            TenantId = budget.TenantId,
                            Title = $"Budget exceeded: {categoryName}",
                            Message = $"You've used {percent}% of your {categoryName} budget (${spending:N2} / ${budget.Amount:N2}).",
                            Type = NotificationType.BudgetAlert,
                            Link = "/budgets",
                            DuplicateKey = key
                        };

                        await _notificationRepo.AddAsync(notification);

                        var user = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.TenantId == budget.TenantId);

                        if (user?.Email is not null)
                        {
                            var html = BuildBudgetAlertEmail(categoryName, spending, budget.Amount, percent);
                            await _emailSender.SendAsync(user.Email, $"Budget Alert: {categoryName} exceeded", html);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing budget alert for budget {Id}", budget.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in budget alert check");
            }
        }

        private async Task CheckLoanRemindersAsync(Persistence.MoneyTrackerDbContext context, DateTime today)
        {
            try
            {
                var lookAheadDays = today.AddDays(3);

                var installments = await context.LoanInstallments
                    .IgnoreQueryFilters()
                    .Include(i => i.Loan)
                    .Where(i => i.Loan != null
                             && !i.Loan.IsDeleted
                             && i.Loan.Status == LoanStatus.Active
                             && i.DueDate.Date >= today.Date
                             && i.DueDate.Date <= lookAheadDays.Date)
                    .ToListAsync();

                foreach (var installment in installments)
                {
                    try
                    {
                        var key = $"loan-reminder-{installment.Id}-{today:yyyy-MM-dd}";
                        if (await _notificationRepo.ExistsByDuplicateKeyTodayAsync(key)) continue;

                        var daysUntilDue = (installment.DueDate.Date - today.Date).Days;
                        var dueLabel = daysUntilDue == 0 ? "today" : $"in {daysUntilDue} day(s)";
                        var contactName = installment.Loan?.ContactName ?? "a loan";

                        var notification = new AppNotification
                        {
                            TenantId = installment.Loan!.TenantId,
                            Title = $"Loan payment due {dueLabel}",
                            Message = $"Installment #{installment.InstallmentNumber} for {contactName} (${installment.ExpectedAmount:N2}) is due on {installment.DueDate:MMM dd, yyyy}.",
                            Type = NotificationType.LoanReminder,
                            Link = "/loans",
                            DuplicateKey = key
                        };

                        await _notificationRepo.AddAsync(notification);

                        var user = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.TenantId == installment.Loan.TenantId);

                        if (user?.Email is not null)
                        {
                            var html = BuildLoanReminderEmail(contactName, installment.InstallmentNumber, installment.ExpectedAmount, installment.DueDate, dueLabel);
                            await _emailSender.SendAsync(user.Email, $"Loan Payment Reminder: due {dueLabel}", html);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing loan reminder for installment {Id}", installment.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in loan reminder check");
            }
        }

        private async Task CheckCreditCardAlertsAsync(MoneyTrackerDbContext context, DateTime today)
        {
            try
            {
                const int CutLeadDays = 7;
                const int DueLeadDays = 5;

                var accounts = await context.Accounts
                    .IgnoreQueryFilters()
                    .Where(a => !a.IsDeleted && a.Type == AccountType.Credit
                             && (a.CutDay.HasValue || a.PaymentDay.HasValue))
                    .ToListAsync();

                foreach (var account in accounts)
                {
                    try
                    {
                        var user = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.TenantId == account.TenantId);

                        if (account.CutDay.HasValue)
                        {
                            var cutDate = GetThisMonthOccurrence(today, account.CutDay.Value);
                            var daysUntil = (cutDate.Date - today.Date).Days;
                            if (daysUntil >= 0 && daysUntil <= CutLeadDays)
                            {
                                var key = $"credit-cut-{account.TenantId}-{account.Id}-{today.Year}-{today.Month:D2}";
                                if (!await _notificationRepo.ExistsByDuplicateKeyAsync(key))
                                {
                                    var label = daysUntil == 0 ? "today" : $"in {daysUntil} day(s)";
                                    await _notificationRepo.AddAsync(new AppNotification
                                    {
                                        TenantId = account.TenantId,
                                        Title = $"Statement closes {label}: {account.Name}",
                                        Message = $"Your statement for '{account.Name}' closes {label} (day {account.CutDay}, {cutDate:MMM dd}). Review your charges.",
                                        Type = NotificationType.CreditCardCut,
                                        Link = $"/accounts/{account.Id}",
                                        DuplicateKey = key
                                    });

                                    if (user?.Email is not null)
                                        await _emailSender.SendAsync(user.Email,
                                            $"Statement closing {label}: {account.Name}",
                                            BuildCreditCutEmail(account.Name, cutDate, daysUntil));
                                }
                            }
                        }

                        if (account.PaymentDay.HasValue && account.Balance < 0)
                        {
                            var dueDate = GetThisMonthOccurrence(today, account.PaymentDay.Value);
                            var daysUntil = (dueDate.Date - today.Date).Days;
                            if (daysUntil >= -3 && daysUntil <= DueLeadDays)
                            {
                                var key = $"credit-due-{account.TenantId}-{account.Id}-{today.Year}-{today.Month:D2}";
                                if (!await _notificationRepo.ExistsByDuplicateKeyAsync(key))
                                {
                                    var label = daysUntil < 0 ? $"was due {Math.Abs(daysUntil)}d ago (OVERDUE)"
                                              : daysUntil == 0 ? "is due today"
                                              : $"is due in {daysUntil} day(s)";
                                    await _notificationRepo.AddAsync(new AppNotification
                                    {
                                        TenantId = account.TenantId,
                                        Title = $"Credit card payment {label}: {account.Name}",
                                        Message = $"Payment of ${Math.Abs(account.Balance):N2} {label} for '{account.Name}' (day {account.PaymentDay}, {dueDate:MMM dd}).",
                                        Type = NotificationType.CreditCardDue,
                                        Link = $"/accounts/{account.Id}",
                                        DuplicateKey = key
                                    });

                                    if (user?.Email is not null)
                                        await _emailSender.SendAsync(user.Email,
                                            $"Credit card payment {label}: {account.Name}",
                                            BuildCreditDueEmail(account.Name, Math.Abs(account.Balance), dueDate, daysUntil));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing credit card alert for account {Id}", account.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in credit card alert check");
            }
        }

        private static DateTime GetThisMonthOccurrence(DateTime today, int day)
        {
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            return new DateTime(today.Year, today.Month, Math.Min(day, daysInMonth));
        }

        private static string BuildCreditCutEmail(string accountName, DateTime cutDate, int daysUntil) =>
            $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:'Segoe UI',sans-serif;background:#f4f7f6;margin:0;padding:32px;">
              <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);">
                <div style="background:#1976D2;padding:24px;text-align:center;">
                  <h2 style="color:#fff;margin:0;">📋 Statement Closing Soon</h2>
                </div>
                <div style="padding:28px 32px;">
                  <p style="font-size:16px;color:#2A364E;">Your statement for <strong>{accountName}</strong> closes {(daysUntil == 0 ? "today" : $"in {daysUntil} day(s)")}.</p>
                  <div style="background:#F0F4FF;border-left:4px solid #1976D2;padding:16px;border-radius:4px;margin:16px 0;">
                    <p style="margin:0;color:#1976D2;font-size:18px;font-weight:bold;">Closing date: {cutDate:MMMM dd, yyyy}</p>
                    <p style="margin:4px 0 0;color:#666;">This will determine your minimum payment due.</p>
                  </div>
                  <a href="/accounts" style="display:inline-block;margin-top:16px;padding:12px 24px;background:#2143B5;color:#fff;border-radius:8px;text-decoration:none;font-weight:600;">Review Account</a>
                </div>
                <div style="background:#f4f7f6;padding:16px;text-align:center;">
                  <p style="margin:0;font-size:12px;color:#999;">MoneyTracker &mdash; Credit Card Alerts</p>
                </div>
              </div>
            </body>
            </html>
            """;

        private static string BuildCreditDueEmail(string accountName, decimal amount, DateTime dueDate, int daysUntil)
        {
            var urgencyColor = daysUntil < 0 ? "#E53935" : daysUntil <= 2 ? "#E53935" : "#FF9800";
            var label = daysUntil < 0 ? $"OVERDUE by {Math.Abs(daysUntil)} day(s)" : daysUntil == 0 ? "due TODAY" : $"due in {daysUntil} day(s)";
            return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:'Segoe UI',sans-serif;background:#f4f7f6;margin:0;padding:32px;">
              <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);">
                <div style="background:{urgencyColor};padding:24px;text-align:center;">
                  <h2 style="color:#fff;margin:0;">💳 Credit Card Payment {label.ToUpper()}</h2>
                </div>
                <div style="padding:28px 32px;">
                  <p style="font-size:16px;color:#2A364E;">Your payment for <strong>{accountName}</strong> is {label}.</p>
                  <div style="background:#FFF3F3;border-left:4px solid {urgencyColor};padding:16px;border-radius:4px;margin:16px 0;">
                    <p style="margin:0;color:{urgencyColor};font-size:24px;font-weight:bold;">${amount:N2}</p>
                    <p style="margin:4px 0 0;color:#666;">Payment date: {dueDate:MMMM dd, yyyy}</p>
                  </div>
                  <a href="/accounts" style="display:inline-block;margin-top:16px;padding:12px 24px;background:#2143B5;color:#fff;border-radius:8px;text-decoration:none;font-weight:600;">Go to Accounts</a>
                </div>
                <div style="background:#f4f7f6;padding:16px;text-align:center;">
                  <p style="margin:0;font-size:12px;color:#999;">MoneyTracker &mdash; Credit Card Alerts</p>
                </div>
              </div>
            </body>
            </html>
            """;
        }

        private static string BuildBudgetAlertEmail(string categoryName, decimal spending, decimal budgetAmount, int percent) => $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:'Segoe UI',sans-serif;background:#f4f7f6;margin:0;padding:32px;">
              <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);">
                <div style="background:#E53935;padding:24px;text-align:center;">
                  <h2 style="color:#fff;margin:0;">⚠️ Budget Alert</h2>
                </div>
                <div style="padding:28px 32px;">
                  <p style="font-size:16px;color:#2A364E;">Your <strong>{categoryName}</strong> budget has been exceeded.</p>
                  <div style="background:#FFF3F3;border-left:4px solid #E53935;padding:16px;border-radius:4px;margin:16px 0;">
                    <p style="margin:0;color:#E53935;font-size:18px;font-weight:bold;">{percent}% used</p>
                    <p style="margin:4px 0 0;color:#666;">${spending:N2} spent of ${budgetAmount:N2} budget</p>
                  </div>
                  <a href="/budgets" style="display:inline-block;margin-top:16px;padding:12px 24px;background:#2143B5;color:#fff;border-radius:8px;text-decoration:none;font-weight:600;">View Budgets</a>
                </div>
                <div style="background:#f4f7f6;padding:16px;text-align:center;">
                  <p style="margin:0;font-size:12px;color:#999;">MoneyTracker &mdash; Budget Alerts</p>
                </div>
              </div>
            </body>
            </html>
            """;

        private static string BuildLoanReminderEmail(string contactName, int installmentNumber, decimal amount, DateTime dueDate, string dueLabel) => $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:'Segoe UI',sans-serif;background:#f4f7f6;margin:0;padding:32px;">
              <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);">
                <div style="background:#1976D2;padding:24px;text-align:center;">
                  <h2 style="color:#fff;margin:0;">💳 Loan Payment Reminder</h2>
                </div>
                <div style="padding:28px 32px;">
                  <p style="font-size:16px;color:#2A364E;">Installment <strong>#{installmentNumber}</strong> for <strong>{contactName}</strong> is due <strong>{dueLabel}</strong>.</p>
                  <div style="background:#F0F4FF;border-left:4px solid #1976D2;padding:16px;border-radius:4px;margin:16px 0;">
                    <p style="margin:0;color:#1976D2;font-size:20px;font-weight:bold;">${amount:N2}</p>
                    <p style="margin:4px 0 0;color:#666;">Due: {dueDate:MMMM dd, yyyy}</p>
                  </div>
                  <a href="/loans" style="display:inline-block;margin-top:16px;padding:12px 24px;background:#2143B5;color:#fff;border-radius:8px;text-decoration:none;font-weight:600;">View Loans</a>
                </div>
                <div style="background:#f4f7f6;padding:16px;text-align:center;">
                  <p style="margin:0;font-size:12px;color:#999;">MoneyTracker &mdash; Loan Reminders</p>
                </div>
              </div>
            </body>
            </html>
            """;
    }
}
