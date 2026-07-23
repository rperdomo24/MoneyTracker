using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Calendar;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Common.Extensions;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Application.Mappers.Calendar;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Calendar;
using MoneyTracker.Domain.Enums.Filters;
using MoneyTracker.Domain.Enums.Loans;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly IAccountService _accountService;
        private readonly IRecurringTransactionService _recurringService;
        private readonly ILoanService _loanService;
        private readonly ISavingsGoalService _goalService;
        private readonly ICalendarReminderService _reminderService;
        private readonly ITransactionService _transactionService;
        private readonly ITimeZoneService _timeZoneService;
        private readonly ILogger<CalendarService> _logger;

        public CalendarService(
            IAccountService accountService,
            IRecurringTransactionService recurringService,
            ILoanService loanService,
            ISavingsGoalService goalService,
            ICalendarReminderService reminderService,
            ITransactionService transactionService,
            ITimeZoneService timeZoneService,
            ILogger<CalendarService> logger)
        {
            _accountService = accountService;
            _recurringService = recurringService;
            _loanService = loanService;
            _goalService = goalService;
            _reminderService = reminderService;
            _transactionService = transactionService;
            _timeZoneService = timeZoneService;
            _logger = logger;
        }

        public async Task<OperationResult<Dictionary<int, List<CalendarEventDto>>>> GetMonthEventsAsync(int year, int month)
        {
            try
            {
                var monthStart = new DateOnly(year, month, 1);
                var monthEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

                var (accountsResult, recurringResult, loansResult, goalsResult, remindersResult) = await LoadAllSourcesAsync(year, month);

                var events = new Dictionary<int, List<CalendarEventDto>>();

                if (accountsResult.Success)
                    AddCreditCardEvents(events, accountsResult.Data!, year, month, monthEnd.Day);

                if (recurringResult.Success)
                    AddRecurringEvents(events, recurringResult.Data!, monthStart, monthEnd);

                if (loansResult.Success)
                    AddLoanEvents(events, loansResult.Data!, monthStart, monthEnd);

                if (goalsResult.Success)
                    AddGoalEvents(events, goalsResult.Data!, monthStart, monthEnd);

                if (remindersResult.Success)
                    AddReminderEvents(events, remindersResult.Data!, monthStart, monthEnd);

                return OperationResult<Dictionary<int, List<CalendarEventDto>>>.Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building calendar events for {Year}/{Month}", year, month);
                return OperationResult<Dictionary<int, List<CalendarEventDto>>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        private async Task<(
            OperationResult<List<Application.DTOs.AccountDto>>,
            OperationResult<List<Application.DTOs.RecurringTransactions.RecurringTransactionDto>>,
            OperationResult<List<Application.DTOs.Loans.LoanDto>>,
            OperationResult<List<Application.DTOs.Goals.SavingsGoalDto>>,
            OperationResult<List<CalendarReminderDto>>)>
            LoadAllSourcesAsync(int year, int month)
        {
            var accountsTask = _accountService.GetAllAsync();
            var recurringTask = _recurringService.GetAllAsync();
            var loansTask = _loanService.GetAllAsync();
            var goalsTask = _goalService.GetAllAsync();
            var remindersTask = _reminderService.GetByMonthAsync(year, month);

            await Task.WhenAll(accountsTask, recurringTask, loansTask, goalsTask, remindersTask);

            return (accountsTask.Result, recurringTask.Result, loansTask.Result, goalsTask.Result, remindersTask.Result);
        }

        private static void AddCreditCardEvents(
            Dictionary<int, List<CalendarEventDto>> events,
            List<Application.DTOs.AccountDto> accounts,
            int year, int month, int daysInMonth)
        {
            foreach (var account in accounts.Where(a => a.Type == AccountType.Credit))
            {
                var fullName = !string.IsNullOrEmpty(account.CardDisplayName)
                    ? $"{account.BankName} · {account.CardDisplayName}"
                    : account.Name;

                if (account.CutDay.HasValue)
                {
                    var day = Math.Min(account.CutDay.Value, daysInMonth);
                    AddEvent(events, day, new CalendarEventDto
                    {
                        Type = CalendarEventType.CreditCut,
                        Title = fullName,
                        Subtitle = "Statement closes",
                        Color = account.Color,
                        EntityId = account.Id,
                        NavigationUrl = $"/accounts/{account.Id}"
                    });
                }

                if (account.PaymentDay.HasValue)
                {
                    var day = Math.Min(account.PaymentDay.Value, daysInMonth);
                    AddEvent(events, day, new CalendarEventDto
                    {
                        Type = CalendarEventType.CreditDue,
                        Title = fullName,
                        Subtitle = account.CurrentBalance < 0 ? $"Payment due · ${Math.Abs(account.CurrentBalance):N2}" : "Payment date",
                        Amount = account.CurrentBalance < 0 ? Math.Abs(account.CurrentBalance) : null,
                        Color = account.Color,
                        EntityId = account.Id,
                        NavigationUrl = $"/accounts/{account.Id}"
                    });
                }
            }
        }

        private void AddRecurringEvents(
            Dictionary<int, List<CalendarEventDto>> events,
            List<Application.DTOs.RecurringTransactions.RecurringTransactionDto> recurringList,
            DateOnly monthStart, DateOnly monthEnd)
        {
            foreach (var rt in recurringList.Where(r => r.IsActive))
            {
                var localNextDate = _timeZoneService.ConvertFromUtc(rt.NextDate);
                var localEndDate = rt.EndDate.HasValue ? _timeZoneService.ConvertFromUtc(rt.EndDate.Value) : (DateTime?)null;
                var occurrences = ProjectOccurrencesInMonth(localNextDate, rt.Frequency, localEndDate, monthStart, monthEnd);
                foreach (var date in occurrences)
                {
                    AddEvent(events, date.Day, new CalendarEventDto
                    {
                        Type = CalendarEventType.Recurring,
                        Title = rt.Name,
                        Subtitle = $"{rt.Frequency} · {rt.AccountName}",
                        Amount = Math.Abs(rt.Amount),
                        EntityId = rt.Id,
                        NavigationUrl = "/recurring"
                    });
                }
            }
        }

        private void AddLoanEvents(
            Dictionary<int, List<CalendarEventDto>> events,
            List<Application.DTOs.Loans.LoanDto> loans,
            DateOnly monthStart, DateOnly monthEnd)
        {
            foreach (var loan in loans.Where(l => l.Status == LoanStatus.Active))
            {
                // Installments take priority; use DueDate only if no installments exist
                if (loan.Installments.Count > 0)
                {
                    foreach (var installment in loan.Installments.Where(i => !i.IsPaid))
                    {
                        var installDate = ToLocalDate(installment.DueDate);
                        if (installDate < monthStart || installDate > monthEnd) continue;

                        AddEvent(events, installDate.Day, new CalendarEventDto
                        {
                            Type = CalendarEventType.LoanInstallment,
                            Title = loan.ContactName,
                            Subtitle = installment.IsOverdue
                                ? $"Overdue · #{installment.InstallmentNumber}"
                                : $"Installment #{installment.InstallmentNumber}",
                            Amount = installment.PendingAmount,
                            EntityId = loan.Id,
                            NavigationUrl = "/loans"
                        });
                    }
                }
                else if (loan.DueDate.HasValue)
                {
                    var dueDate = ToLocalDate(loan.DueDate.Value);
                    if (dueDate >= monthStart && dueDate <= monthEnd)
                    {
                        AddEvent(events, dueDate.Day, new CalendarEventDto
                        {
                            Type = CalendarEventType.LoanDue,
                            Title = loan.ContactName,
                            Subtitle = loan.IsOverdue ? "Overdue" : "Payment due",
                            Amount = loan.Balance > 0 ? loan.Balance : null,
                            EntityId = loan.Id,
                            NavigationUrl = "/loans"
                        });
                    }
                }
            }
        }

        private void AddGoalEvents(
            Dictionary<int, List<CalendarEventDto>> events,
            List<Application.DTOs.Goals.SavingsGoalDto> goals,
            DateOnly monthStart, DateOnly monthEnd)
        {
            foreach (var goal in goals.Where(g => !g.IsCompleted && g.TargetDate.HasValue))
            {
                var targetDate = ToLocalDate(goal.TargetDate!.Value);
                if (targetDate < monthStart || targetDate > monthEnd) continue;

                AddEvent(events, targetDate.Day, new CalendarEventDto
                {
                    Type = CalendarEventType.GoalDeadline,
                    Title = goal.Name,
                    Subtitle = $"Goal deadline · {goal.ProgressPercent}% reached",
                    Amount = goal.TargetAmount - goal.CurrentAmount,
                    Color = goal.Color,
                    EntityId = goal.Id,
                    NavigationUrl = "/goals"
                });
            }
        }

        private void AddReminderEvents(
            Dictionary<int, List<CalendarEventDto>> events,
            List<CalendarReminderDto> reminders,
            DateOnly monthStart, DateOnly monthEnd)
        {
            foreach (var reminder in reminders)
            {
                var localDate = _timeZoneService.ConvertFromUtc(reminder.Date);
                var localEndDate = reminder.RecurrenceEndDate.HasValue
                    ? _timeZoneService.ConvertFromUtc(reminder.RecurrenceEndDate.Value)
                    : (DateTime?)null;

                var occurrences = reminder.IsRecurring && reminder.RecurrenceFrequency.HasValue
                    ? ProjectOccurrencesInMonth(localDate, reminder.RecurrenceFrequency.Value, localEndDate, monthStart, monthEnd)
                    : GetSingleOccurrence(localDate, monthStart, monthEnd);

                foreach (var date in occurrences)
                {
                    AddEvent(events, date.Day, new CalendarEventDto
                    {
                        Type = CalendarEventType.Reminder,
                        Title = reminder.Title,
                        Subtitle = reminder.Notes,
                        Color = reminder.Color,
                        EntityId = reminder.Id
                    });
                }
            }
        }

        private static List<DateOnly> ProjectOccurrencesInMonth(
            DateTime referenceDate,
            RecurringFrequency frequency,
            DateTime? endDate,
            DateOnly monthStart,
            DateOnly monthEnd)
        {
            var results = new List<DateOnly>();
            var anchor = DateOnly.FromDateTime(referenceDate.Date);
            var cutoff = endDate.HasValue ? DateOnly.FromDateTime(endDate.Value.Date) : monthEnd;
            var effectiveEnd = cutoff < monthEnd ? cutoff : monthEnd;

            // Walk backwards from anchor until before monthStart to find earliest occurrence in month
            while (anchor > monthStart)
            {
                var prev = StepBackward(anchor, frequency);
                if (prev < monthStart) break;
                anchor = prev;
            }

            // If anchor overshot to before month, advance forward to find first hit
            while (anchor < monthStart)
                anchor = StepForward(anchor, frequency);

            // Collect all occurrences in [monthStart, effectiveEnd]
            var current = anchor;
            while (current <= effectiveEnd)
            {
                results.Add(current);
                current = StepForward(current, frequency);
            }

            return results;
        }

        private static List<DateOnly> GetSingleOccurrence(DateTime date, DateOnly monthStart, DateOnly monthEnd)
        {
            var d = DateOnly.FromDateTime(date.Date);
            return d >= monthStart && d <= monthEnd ? [d] : [];
        }

        private static DateOnly StepForward(DateOnly date, RecurringFrequency frequency) =>
            frequency switch
            {
                RecurringFrequency.Daily => date.AddDays(1),
                RecurringFrequency.Weekly => date.AddDays(7),
                RecurringFrequency.BiWeekly => date.AddDays(14),
                RecurringFrequency.Monthly => date.AddMonths(1),
                RecurringFrequency.BiMonthly => date.AddMonths(2),
                RecurringFrequency.Quarterly => date.AddMonths(3),
                RecurringFrequency.Yearly => date.AddYears(1),
                _ => date.AddMonths(1)
            };

        private static DateOnly StepBackward(DateOnly date, RecurringFrequency frequency) =>
            frequency switch
            {
                RecurringFrequency.Daily => date.AddDays(-1),
                RecurringFrequency.Weekly => date.AddDays(-7),
                RecurringFrequency.BiWeekly => date.AddDays(-14),
                RecurringFrequency.Monthly => date.AddMonths(-1),
                RecurringFrequency.BiMonthly => date.AddMonths(-2),
                RecurringFrequency.Quarterly => date.AddMonths(-3),
                RecurringFrequency.Yearly => date.AddYears(-1),
                _ => date.AddMonths(-1)
            };

        private DateOnly ToLocalDate(DateTime utcDate) =>
            DateOnly.FromDateTime(_timeZoneService.ConvertFromUtc(utcDate).Date);

        private static void AddEvent(Dictionary<int, List<CalendarEventDto>> events, int day, CalendarEventDto evt)
        {
            if (!events.ContainsKey(day)) events[day] = new List<CalendarEventDto>();
            events[day].Add(evt);
        }

        public async Task<OperationResult<Dictionary<int, (decimal Income, decimal Expense)>>> GetMonthDayTotalsAsync(int year, int month)
        {
            try
            {
                // Pass local dates — TimeRangeService.GetCustomRangeUtc handles UTC conversion internally
                var result = await _transactionService.GetFilteredAsync(new TransactionFilterDto
                {
                    TimePeriod  = TimePeriodFilter.Custom,
                    FromDate    = new DateTime(year, month, 1),
                    ToDate      = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59),
                    RowsPerPage = 2000,
                    SkipSorting = true
                });

                if (!result.Success)
                    return OperationResult<Dictionary<int, (decimal Income, decimal Expense)>>.Ok(new());

                var totals = new Dictionary<int, (decimal Income, decimal Expense)>();

                foreach (var tx in result.Data!.Transactions)
                {
                    // mirrors TransactionService.GetFilteredAsync: exclude transfers from balance
                    if (!tx.IsIncome() && !tx.IsExpense()) continue;

                    var day = tx.Date.Day; // already local via MapToDto
                    totals.TryGetValue(day, out var existing);

                    if (tx.IsIncome())
                        totals[day] = (existing.Income + tx.Amount, existing.Expense);
                    else
                        totals[day] = (existing.Income, existing.Expense + Math.Abs(tx.Amount));
                }

                return OperationResult<Dictionary<int, (decimal Income, decimal Expense)>>.Ok(totals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching month day totals for {Year}/{Month}", year, month);
                return OperationResult<Dictionary<int, (decimal Income, decimal Expense)>>.Ok(new());
            }
        }

        public async Task<OperationResult<TransactionSummaryDto>> GetDayBalanceAsync(DateOnly date)
        {
            try
            {
                // Pass local dates — TimeRangeService.GetCustomRangeUtc handles UTC conversion internally
                return await _transactionService.GetFilteredAsync(new TransactionFilterDto
                {
                    TimePeriod  = TimePeriodFilter.Custom,
                    FromDate    = date.ToDateTime(TimeOnly.MinValue),
                    ToDate      = date.ToDateTime(new TimeOnly(23, 59, 59)),
                    RowsPerPage = 500,
                    SkipSorting = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching day balance for {Date}", date);
                return OperationResult<TransactionSummaryDto>.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
