using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Loans;
using MoneyTracker.Domain.Enums.Transaction;
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
        private readonly ITimeZoneService _timeZoneService;
        private readonly ApplicationSettings _applicationSettings;
        private readonly ILogger<NotificationGeneratorJob> _logger;

        public NotificationGeneratorJob(
            IServiceScopeFactory scopeFactory,
            INotificationRepository notificationRepo,
            IEmailSenderService emailSender,
            UserManager<ApplicationUser> userManager,
            ITimeZoneService timeZoneService,
            IOptions<ApplicationSettings> applicationSettings,
            ILogger<NotificationGeneratorJob> logger)
        {
            _scopeFactory = scopeFactory;
            _notificationRepo = notificationRepo;
            _emailSender = emailSender;
            _userManager = userManager;
            _timeZoneService = timeZoneService;
            _applicationSettings = applicationSettings.Value;
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

            var created = new List<AppNotification>();

            await CheckBudgetAlertsAsync(context, today, created);
            await CheckLoanRemindersAsync(context, today, created);
            await CheckCreditCardAlertsAsync(context, today, created);
            await CheckCalendarRemindersAsync(context, today, created);
            await CheckSavingsGoalContributionRemindersAsync(context, today, created);

            await SendDigestEmailsAsync(created);
        }

        private async Task SendDigestEmailsAsync(List<AppNotification> created)
        {
            if (created.Count == 0) return;

            foreach (var group in created.GroupBy(n => n.TenantId))
            {
                try
                {
                    var user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.TenantId == group.Key);

                    if (user?.Email is null) continue;

                    var items = group.ToList();
                    var subject = NotificationDigestEmailBuilder.BuildSubject(items.Count);
                    var html = NotificationDigestEmailBuilder.Build(items, _applicationSettings.PublicBaseUrl);

                    var result = await _emailSender.SendAsync(user.Email, subject, html);
                    if (!result.Success)
                        _logger.LogWarning("Notification digest email failed for tenant {TenantId}. Reason: {Reason}", group.Key, result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending notification digest for tenant {TenantId}", group.Key);
                }
            }
        }

        private async Task CheckBudgetAlertsAsync(Persistence.MoneyTrackerDbContext context, DateTime today, List<AppNotification> created)
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

                        // Monthly key: only alert once per budget per month, not every day it stays over.
                        var key = $"budget-alert-{budget.Id}-{today.Year}-{today.Month:D2}";
                        if (await _notificationRepo.ExistsByDuplicateKeyAsync(key)) continue;

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
                        created.Add(notification);
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

        private async Task CheckLoanRemindersAsync(Persistence.MoneyTrackerDbContext context, DateTime today, List<AppNotification> created)
        {
            try
            {
                var lookAheadDays = today.AddDays(3);

                var loans = await context.Loans
                    .IgnoreQueryFilters()
                    .Include(l => l.Installments)
                    .Include(l => l.Payments)
                    .Where(l => !l.IsDeleted
                             && l.Status == LoanStatus.Active
                             && l.Installments.Any(i => i.DueDate.Date >= today.Date && i.DueDate.Date <= lookAheadDays.Date))
                    .ToListAsync();

                foreach (var loan in loans)
                {
                    var dueInstallments = loan.MapToDto().Installments
                        .Where(i => !i.IsPaid && i.DueDate.Date >= today.Date && i.DueDate.Date <= lookAheadDays.Date);

                    foreach (var installment in dueInstallments)
                    {
                        try
                        {
                            var key = $"loan-reminder-{installment.Id}-{today:yyyy-MM-dd}";
                            if (await _notificationRepo.ExistsByDuplicateKeyTodayAsync(key)) continue;

                            var daysUntilDue = (installment.DueDate.Date - today.Date).Days;
                            var dueLabel = daysUntilDue == 0 ? "today" : $"in {daysUntilDue} day(s)";
                            var contactName = loan.ContactName;

                            var notification = new AppNotification
                            {
                                TenantId = loan.TenantId,
                                Title = $"Loan payment due {dueLabel}",
                                Message = $"Installment #{installment.InstallmentNumber} for {contactName} (${installment.ExpectedAmount:N2}) is due on {installment.DueDate:MMM dd, yyyy}.",
                                Type = NotificationType.LoanReminder,
                                Link = "/loans",
                                DuplicateKey = key
                            };

                            await _notificationRepo.AddAsync(notification);
                            created.Add(notification);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing loan reminder for installment {Id}", installment.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in loan reminder check");
            }
        }

        private async Task CheckCreditCardAlertsAsync(MoneyTrackerDbContext context, DateTime today, List<AppNotification> created)
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
                                    var notification = new AppNotification
                                    {
                                        TenantId = account.TenantId,
                                        Title = $"Statement closes {label}: {account.Name}",
                                        Message = $"Your statement for '{account.Name}' closes {label} (day {account.CutDay}, {cutDate:MMM dd}). Review your charges.",
                                        Type = NotificationType.CreditCardCut,
                                        Link = $"/accounts/{account.Id}",
                                        DuplicateKey = key
                                    };
                                    await _notificationRepo.AddAsync(notification);
                                    created.Add(notification);
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
                                    var notification = new AppNotification
                                    {
                                        TenantId = account.TenantId,
                                        Title = $"Credit card payment {label}: {account.Name}",
                                        Message = $"Payment of ${Math.Abs(account.Balance):N2} {label} for '{account.Name}' (day {account.PaymentDay}, {dueDate:MMM dd}).",
                                        Type = NotificationType.CreditCardDue,
                                        Link = $"/accounts/{account.Id}",
                                        DuplicateKey = key
                                    };
                                    await _notificationRepo.AddAsync(notification);
                                    created.Add(notification);
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

        private async Task CheckCalendarRemindersAsync(MoneyTrackerDbContext context, DateTime today, List<AppNotification> created)
        {
            try
            {
                var reminders = await context.CalendarReminders
                    .IgnoreQueryFilters()
                    .Where(r => !r.IsDeleted && r.Date.Date <= today.Date &&
                                (!r.RecurrenceEndDate.HasValue || r.RecurrenceEndDate.Value.Date >= today.Date))
                    .ToListAsync();

                foreach (var reminder in reminders)
                {
                    try
                    {
                        var fires = reminder.IsRecurring && reminder.RecurrenceFrequency.HasValue
                            ? IsOccurrenceToday(reminder.Date, reminder.RecurrenceFrequency.Value, today)
                            : reminder.Date.Date == today.Date;

                        if (!fires) continue;

                        var key = $"calendar-reminder-{reminder.Id}-{today:yyyy-MM-dd}";
                        if (await _notificationRepo.ExistsByDuplicateKeyTodayAsync(key)) continue;

                        var notification = new AppNotification
                        {
                            TenantId = reminder.TenantId,
                            Title = $"Reminder: {reminder.Title}",
                            Message = string.IsNullOrWhiteSpace(reminder.Notes)
                                ? $"You have a reminder today: {reminder.Title}"
                                : $"{reminder.Title} — {reminder.Notes}",
                            Type = NotificationType.CalendarReminder,
                            Link = "/calendar",
                            DuplicateKey = key
                        };

                        await _notificationRepo.AddAsync(notification);
                        created.Add(notification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing calendar reminder {Id}", reminder.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in calendar reminder check");
            }
        }

        private async Task CheckSavingsGoalContributionRemindersAsync(MoneyTrackerDbContext context, DateTime today, List<AppNotification> created)
        {
            try
            {
                var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

                var goals = await context.SavingsGoals
                    .IgnoreQueryFilters()
                    .Where(g => !g.IsDeleted && !g.IsCompleted && g.ContributionReminderDay.HasValue)
                    .ToListAsync();

                foreach (var goal in goals)
                {
                    try
                    {
                        var reminderDay = Math.Min(goal.ContributionReminderDay!.Value, daysInMonth);
                        if (today.Day != reminderDay) continue;

                        var key = $"goal-contribution-{goal.Id}-{today:yyyy-MM}";
                        if (await _notificationRepo.ExistsByDuplicateKeyAsync(key)) continue;

                        var amountText = goal.ContributionReminderAmount.HasValue
                            ? $"${goal.ContributionReminderAmount.Value:N2}"
                            : "your planned amount";

                        var notification = new AppNotification
                        {
                            TenantId = goal.TenantId,
                            Title = $"Savings reminder: {goal.Name}",
                            Message = $"Time to save {amountText} toward your goal '{goal.Name}'.",
                            Type = NotificationType.GoalContributionReminder,
                            Link = "/goals",
                            DuplicateKey = key
                        };

                        await _notificationRepo.AddAsync(notification);
                        created.Add(notification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing contribution reminder for goal {Id}", goal.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in savings goal contribution reminder check");
            }
        }

        private static bool IsOccurrenceToday(DateTime reminderDate, RecurringFrequency frequency, DateTime today)
        {
            var current = reminderDate.Date;
            while (current < today.Date)
                current = StepReminderForward(current, frequency);
            return current == today.Date;
        }

        private static DateTime StepReminderForward(DateTime date, RecurringFrequency frequency) =>
            frequency switch
            {
                RecurringFrequency.Daily     => date.AddDays(1),
                RecurringFrequency.Weekly    => date.AddDays(7),
                RecurringFrequency.BiWeekly  => date.AddDays(14),
                RecurringFrequency.Monthly   => date.AddMonths(1),
                RecurringFrequency.BiMonthly => date.AddMonths(2),
                RecurringFrequency.Quarterly => date.AddMonths(3),
                RecurringFrequency.Yearly    => date.AddYears(1),
                _                            => date.AddMonths(1)
            };

        private static DateTime GetThisMonthOccurrence(DateTime today, int day)
        {
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            return new DateTime(today.Year, today.Month, Math.Min(day, daysInMonth));
        }
    }
}
