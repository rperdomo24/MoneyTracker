using MoneyTracker.Application.Common;
using MoneyTracker.Application.Common.Extensions;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Budgets;
using MoneyTracker.Application.DTOs.Dashboard;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Filters;
using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MoneyTracker.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private const int DashboardMaxRangeDays = 730;
        private readonly IAccountService _accountService;
        private readonly ITransactionService _transactionService;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IBudgetService _budgetService;
        private readonly ITimeZoneService _timeZoneService;
        private readonly ILogger<DashboardService> _logger;
        private readonly IErrorLogService? _errorLogService;

        public DashboardService(
            IAccountService accountService,
            ITransactionService transactionService,
            ITransactionRepository transactionRepository,
            IBudgetService budgetService,
            ITimeZoneService timeZoneService,
            ILogger<DashboardService> logger,
            IErrorLogService? errorLogService = null)
        {
            _accountService = accountService;
            _transactionService = transactionService;
            _transactionRepository = transactionRepository;
            _budgetService = budgetService;
            _timeZoneService = timeZoneService;
            _logger = logger;
            _errorLogService = errorLogService;
        }

        public async Task<OperationResult<DashboardOverviewDto>> GetOverviewAsync(
            DashboardFilterDto? filter = null,
            int recentTransactionsCount = 10,
            int balanceTrendMonths = 6)
        {
            try
            {
                _ = balanceTrendMonths;
                filter ??= new DashboardFilterDto();
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var normalizedFilter = NormalizeOverviewFilter(filter, now);

                var accountsResult = await _accountService.GetAccountsWithBalancesAsync();
                if (!accountsResult.Success)
                    return OperationResult<DashboardOverviewDto>.Fail(accountsResult.Message);
                if (accountsResult.Data is null)
                    return OperationResult<DashboardOverviewDto>.Fail("Accounts data is empty");

                var accounts = accountsResult.Data;
                var filteredAccounts = ApplyAccountFilter(accounts, filter.AccountIds);

                var transactionsResult = await _transactionService.GetFilteredAsync(new TransactionFilterDto
                {
                    TimePeriod = normalizedFilter.TimePeriod,
                    FromDate = normalizedFilter.FromDate,
                    ToDate = normalizedFilter.ToDate,
                    AccountIds = normalizedFilter.AccountIds,
                    SkipSorting = true
                });

                if (!transactionsResult.Success)
                    return OperationResult<DashboardOverviewDto>.Fail(transactionsResult.Message);
                if (transactionsResult.Data is null)
                    return OperationResult<DashboardOverviewDto>.Fail("Transactions data is empty");

                var transactions = ApplyTransactionFilter(
                    transactionsResult.Data.Transactions,
                    normalizedFilter.TransactionFilter);

                var netWorthTrend = await BuildBalanceTrendAsync(
                    filteredAccounts,
                    transactions,
                    normalizedFilter,
                    now);
                var spendingTrend = BuildSpendingTrend(transactions, normalizedFilter, now);
                var budgetSummary = await BuildBudgetSummaryAsync(now);

                var overview = new DashboardOverviewDto
                {
                    Summary = BuildSummary(filteredAccounts),
                    CashFlow = BuildCashFlow(transactions, now),
                    BudgetSummary = budgetSummary,
                    CategoryBreakdown = BuildCategoryBreakdown(transactions),
                    RecentTransactions = BuildRecentTransactions(transactions, recentTransactionsCount, now),
                    BalanceTrend = netWorthTrend,
                    SpendingTrend = spendingTrend
                };

                return OperationResult<DashboardOverviewDto>.Ok(overview, "Dashboard overview retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<DashboardOverviewDto>(
                    ex,
                    OperationMessages.UnexpectedError,
                    ServiceMessages.DashboardOverviewError);
            }
        }

        public async Task<OperationResult<DashboardWidgetsDto>> GetOverviewWidgetsAsync(
            DashboardFilterDto? filter = null,
            int recentTransactionsCount = 10)
        {
            try
            {
                filter ??= new DashboardFilterDto();
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var normalizedFilter = NormalizeOverviewFilter(filter, now);
                var entries = await LoadDashboardEntriesAsync(normalizedFilter, now);
                var filteredEntries = ApplyDashboardEntryFilter(entries, normalizedFilter.TransactionFilter);
                var budgetSummary = await BuildBudgetSummaryAsync(now);

                var widgets = new DashboardWidgetsDto
                {
                    CashFlow = BuildCashFlow(filteredEntries, now),
                    BudgetSummary = budgetSummary,
                    SpendingTrend = BuildSpendingTrend(filteredEntries, normalizedFilter, now),
                    CategoryBreakdown = BuildCategoryBreakdown(filteredEntries),
                    RecentTransactions = BuildRecentTransactions(filteredEntries, recentTransactionsCount, now)
                };

                return OperationResult<DashboardWidgetsDto>.Ok(widgets, "Dashboard widgets retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<DashboardWidgetsDto>(
                    ex,
                    OperationMessages.UnexpectedError,
                    ServiceMessages.DashboardWidgetsError);
            }
        }

        public async Task<OperationResult<DashboardBudgetSummaryDto>> GetBudgetSummaryAsync(DashboardFilterDto? filter = null)
        {
            try
            {
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var budgetReferenceDate = ResolveBudgetReferenceDate(filter, now);
                var summary = await BuildBudgetSummaryAsync(budgetReferenceDate);
                return OperationResult<DashboardBudgetSummaryDto>.Ok(summary, "Budget summary retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<DashboardBudgetSummaryDto>(
                    ex,
                    OperationMessages.UnexpectedError,
                    "Error retrieving budget summary.");
            }
        }

        public async Task<OperationResult<List<DashboardSpendingTrendDto>>> GetSpendingTrendAsync(DashboardFilterDto filter)
        {
            try
            {
                filter ??= new DashboardFilterDto();
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var normalizedFilter = NormalizeOverviewFilter(filter, now);
                var (rangeStart, rangeEnd) = ResolveRangeLocal(normalizedFilter, now);

                if (normalizedFilter.TransactionFilter == DashboardTransactionFilter.Income)
                {
                    var emptyTrend = BuildSpendingTrend(
                        new List<DashboardAmountByDateEntry>(),
                        normalizedFilter,
                        now);
                    return OperationResult<List<DashboardSpendingTrendDto>>.Ok(emptyTrend, "Spending trend retrieved successfully");
                }

                var fromUtc = _timeZoneService.ConvertToUtc(rangeStart.Date);
                var toUtc = _timeZoneService.ConvertToUtc(rangeEnd.Date.AddDays(1).AddTicks(-1));
                var entries = await _transactionRepository.GetDashboardEntriesAsync(
                    fromUtc,
                    toUtc,
                    normalizedFilter.AccountIds ?? new List<int>());
                var filteredEntries = ApplyDashboardEntryFilter(entries, normalizedFilter.TransactionFilter);
                var trend = BuildSpendingTrend(filteredEntries, normalizedFilter, now);
                return OperationResult<List<DashboardSpendingTrendDto>>.Ok(trend, "Spending trend retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<List<DashboardSpendingTrendDto>>(
                    ex,
                    OperationMessages.UnexpectedError,
                    "Error retrieving spending trend.");
            }
        }

        public async Task<OperationResult<CashFlowDto>> GetCashFlowAsync(DashboardFilterDto filter)
        {
            try
            {
                filter ??= new DashboardFilterDto();

                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var normalizedFilter = NormalizeOverviewFilter(filter, now);

                var (rangeStart, rangeEnd) = ResolveRangeLocal(normalizedFilter, now);
                var fromUtc = _timeZoneService.ConvertToUtc(rangeStart.Date);
                var toUtc = _timeZoneService.ConvertToUtc(rangeEnd.Date.AddDays(1).AddTicks(-1));
                var accountIds = normalizedFilter.AccountIds ?? new List<int>();

                var aggregates = await _transactionRepository.GetCashFlowAggregatesAsync(
                    fromUtc,
                    toUtc,
                    accountIds) ?? new List<DashboardCashFlowAggregateEntry>();

                var incomeCategories = normalizedFilter.TransactionFilter == DashboardTransactionFilter.Expense
                    ? new List<DashboardCategoryAggregateEntry>()
                    : aggregates
                        .Where(x => x.CategoryType == CategoryTypeEnum.Income)
                        .Select(x => new DashboardCategoryAggregateEntry
                        {
                            CategoryId = x.CategoryId,
                            CategoryName = x.CategoryName,
                            Amount = x.Amount,
                            TransactionCount = x.TransactionCount,
                            CategoryColor = DashboardConstants.DefaultCategoryColor
                        })
                        .ToList();

                var expenseCategories = normalizedFilter.TransactionFilter == DashboardTransactionFilter.Income
                    ? new List<DashboardCategoryAggregateEntry>()
                    : aggregates
                        .Where(x => x.CategoryType == CategoryTypeEnum.Expense)
                        .Select(x => new DashboardCategoryAggregateEntry
                        {
                            CategoryId = x.CategoryId,
                            CategoryName = x.CategoryName,
                            Amount = x.Amount,
                            TransactionCount = x.TransactionCount,
                            CategoryColor = DashboardConstants.DefaultCategoryColor
                        })
                        .ToList();

                var cashFlow = BuildCashFlow(incomeCategories, expenseCategories, now);
                return OperationResult<CashFlowDto>.Ok(cashFlow, "Cash flow retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<CashFlowDto>(
                    ex,
                    OperationMessages.UnexpectedError,
                    "Error retrieving cash flow.");
            }
        }

        public async Task<OperationResult<List<CategoryBreakdownDto>>> GetCategoryBreakdownAsync(DashboardFilterDto filter)
        {
            try
            {
                filter ??= new DashboardFilterDto();
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var normalizedFilter = NormalizeOverviewFilter(filter, now);
                if (normalizedFilter.TransactionFilter == DashboardTransactionFilter.Income)
                {
                    return OperationResult<List<CategoryBreakdownDto>>.Ok(new List<CategoryBreakdownDto>(), "Category breakdown retrieved successfully");
                }

                var (rangeStart, rangeEnd) = ResolveRangeLocal(normalizedFilter, now);
                var fromUtc = _timeZoneService.ConvertToUtc(rangeStart.Date);
                var toUtc = _timeZoneService.ConvertToUtc(rangeEnd.Date.AddDays(1).AddTicks(-1));
                var aggregates = await _transactionRepository.GetCategoryAggregatesAsync(
                    fromUtc,
                    toUtc,
                    normalizedFilter.AccountIds ?? new List<int>(),
                    CategoryTypeEnum.Expense,
                    top: 6);
                var breakdown = BuildCategoryBreakdown(aggregates);
                return OperationResult<List<CategoryBreakdownDto>>.Ok(breakdown, "Category breakdown retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<List<CategoryBreakdownDto>>(
                    ex,
                    OperationMessages.UnexpectedError,
                    "Error retrieving category breakdown.");
            }
        }

        public async Task<OperationResult<List<RecentTransactionDto>>> GetRecentTransactionsAsync(
            DashboardFilterDto filter,
            int count = 10)
        {
            try
            {
                filter ??= new DashboardFilterDto();
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var normalizedFilter = NormalizeOverviewFilter(filter, now);
                var (rangeStart, rangeEnd) = ResolveRangeLocal(normalizedFilter, now);
                var fromUtc = _timeZoneService.ConvertToUtc(rangeStart.Date);
                var toUtc = _timeZoneService.ConvertToUtc(rangeEnd.Date.AddDays(1).AddTicks(-1));

                var (categoryType, includeTransfers) = normalizedFilter.TransactionFilter switch
                {
                    DashboardTransactionFilter.Income => ((CategoryTypeEnum?)CategoryTypeEnum.Income, false),
                    DashboardTransactionFilter.Expense => ((CategoryTypeEnum?)CategoryTypeEnum.Expense, false),
                    _ => ((CategoryTypeEnum?)null, true)
                };

                var recentEntries = await _transactionRepository.GetRecentDashboardEntriesAsync(
                    fromUtc,
                    toUtc,
                    normalizedFilter.AccountIds ?? new List<int>(),
                    categoryType,
                    includeTransfers,
                    count);

                var recent = BuildRecentTransactions(recentEntries, count, now);
                return OperationResult<List<RecentTransactionDto>>.Ok(recent, "Recent transactions retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<List<RecentTransactionDto>>(
                    ex,
                    OperationMessages.UnexpectedError,
                    "Error retrieving recent transactions.");
            }
        }

        public async Task<OperationResult<DashboardSummaryDto>> GetSummaryAsync()
        {
            try
            {
                var accountsResult = await _accountService.GetAccountsWithBalancesAsync();
                if (!accountsResult.Success)
                    return OperationResult<DashboardSummaryDto>.Fail(accountsResult.Message);

                var summary = BuildSummary(accountsResult.Data ?? new List<AccountDto>());

                return OperationResult<DashboardSummaryDto>.Ok(summary, "Dashboard summary retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<DashboardSummaryDto>(
                    ex,
                    OperationMessages.UnexpectedError,
                    ServiceMessages.DashboardSummaryError);
            }
        }

        public async Task<OperationResult<CashFlowDto>> GetCashFlowAsync(TimePeriodFilter period = TimePeriodFilter.ThisMonth)
        {
            return await GetCashFlowAsync(new DashboardFilterDto
            {
                TimePeriod = period
            });
        }

        public async Task<OperationResult<List<FinancialAlertDto>>> GetAlertsAsync()
        {
            try
            {
                var alerts = new List<FinancialAlertDto>();

                var accountsResult = await _accountService.GetAccountsWithBalancesAsync();
                if (!accountsResult.Success)
                    return OperationResult<List<FinancialAlertDto>>.Fail(accountsResult.Message);

                foreach (var account in accountsResult.Data)
                {
                    // Credit limit alerts
                    if (account.Type == AccountType.Credit && account.CreditLimit > 0)
                    {
                        var utilization = Math.Abs(account.CurrentBalance) / account.CreditLimit * 100;

                        if (utilization >= 90)
                        {
                            alerts.Add(new FinancialAlertDto
                            {
                                Type = "CreditLimit",
                                AccountName = account.Name,
                                Message = $"{account.Name} al {utilization:F0}% del límite",
                                Icon = "🔴",
                                Color = "#f44336",
                                IsUrgent = true,
                                Amount = Math.Abs(account.CurrentBalance),
                                Limit = account.CreditLimit
                            });
                        }
                        else if (utilization >= 75)
                        {
                            alerts.Add(new FinancialAlertDto
                            {
                                Type = "CreditLimit",
                                AccountName = account.Name,
                                Message = $"{account.Name} al {utilization:F0}% del límite",
                                Icon = "🟡",
                                Color = "#ff9800",
                                IsUrgent = false,
                                Amount = Math.Abs(account.CurrentBalance),
                                Limit = account.CreditLimit
                            });
                        }
                    }

                    // Low balance alerts for non-credit accounts
                    if (account.Type != AccountType.Credit && account.CurrentBalance < 500 && account.CurrentBalance > 0)
                    {
                        alerts.Add(new FinancialAlertDto
                        {
                            Type = "LowBalance",
                            AccountName = account.Name,
                            Message = $"{account.Name} con balance bajo",
                            Icon = "🟡",
                            Color = "#ff9800",
                            IsUrgent = account.CurrentBalance < 100,
                            Amount = account.CurrentBalance
                        });
                    }
                }

                // REMOVED: High activity alerts to avoid recursion
                // This was calling GetAccountActivityAsync() which creates circular dependencies

                // Add positive alert if no issues
                if (!alerts.Any())
                {
                    alerts.Add(new FinancialAlertDto
                    {
                        Type = "Healthy",
                        AccountName = "Sistema",
                        Message = "Todas las cuentas están saludables",
                        Icon = "🟢",
                        Color = "#4caf50",
                        IsUrgent = false
                    });
                }

                return OperationResult<List<FinancialAlertDto>>.Ok(alerts, "Financial alerts retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<List<FinancialAlertDto>>(
                    ex,
                    OperationMessages.UnexpectedError,
                    ServiceMessages.DashboardAlertsError);
            }
        }

        public async Task<OperationResult<List<CategoryBreakdownDto>>> GetCategoryBreakdownAsync(TimePeriodFilter period = TimePeriodFilter.ThisMonth)
        {
            return await GetCategoryBreakdownAsync(new DashboardFilterDto
            {
                TimePeriod = period
            });
        }

        public async Task<OperationResult<List<AccountActivityDto>>> GetAccountActivityAsync()
        {
            try
            {
                var accountsResult = await _accountService.GetAccountsWithBalancesAsync();
                if (!accountsResult.Success)
                    return OperationResult<List<AccountActivityDto>>.Fail(accountsResult.Message);

                // OPTIMIZED: Get only current month transactions instead of ALL transactions
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var filter = new TransactionFilterDto
                {
                    TimePeriod = TimePeriodFilter.ThisMonth,
                    SkipSorting = true
                };

                var transactionsResult = await _transactionService.GetFilteredAsync(filter);
                if (!transactionsResult.Success)
                    return OperationResult<List<AccountActivityDto>>.Fail(transactionsResult.Message);

                var activities = new List<AccountActivityDto>();

                foreach (var account in accountsResult.Data)
                {
                    var accountTransactions = transactionsResult.Data.Transactions
                        .Where(t => t.AccountId == account.Id)
                        .ToList();

                    var transactionCount = accountTransactions.Count;
                    var totalVolume = accountTransactions.Sum(t => Math.Abs(t.Amount));
                    var lastActivity = accountTransactions.Any()
                        ? accountTransactions.Max(t => t.Date)
                        : (DateTime?)null;

                    string activityLevel = transactionCount switch
                    {
                        > 20 => "High",
                        > 10 => "Medium",
                        > 0 => "Low",
                        _ => "None"
                    };

                    var creditUtilization = account.Type == AccountType.Credit && account.CreditLimit > 0
                        ? (Math.Abs(account.CurrentBalance) / account.CreditLimit) * 100
                        : 0;

                    activities.Add(new AccountActivityDto
                    {
                        AccountId = account.Id,
                        AccountName = account.Name,
                        Type = account.Type,
                        Icon = account.Icon,
                        Color = account.Color ?? DashboardConstants.DefaultCategoryColor,
                        CurrentBalance = account.CurrentBalance,
                        CreditLimit = account.CreditLimit,
                        TransactionCount = transactionCount,
                        TotalVolume = totalVolume,
                        LastActivity = lastActivity,
                        ActivityLevel = activityLevel,
                        CreditUtilization = creditUtilization
                    });
                }

                return OperationResult<List<AccountActivityDto>>.Ok(
                    activities.OrderByDescending(a => a.TransactionCount).ToList(),
                    "Account activity retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<List<AccountActivityDto>>(
                    ex,
                    OperationMessages.UnexpectedError,
                    ServiceMessages.DashboardAccountActivityError);
            }
        }

        public async Task<OperationResult<List<RecentTransactionDto>>> GetRecentTransactionsAsync(int count = 10)
        {
            return await GetRecentTransactionsAsync(new DashboardFilterDto
            {
                TimePeriod = TimePeriodFilter.LastMonth
            }, count);
        }

        public async Task<OperationResult<List<BalanceTrendDto>>> GetBalanceTrendAsync(int months = 6)
        {
            try
            {
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var normalizedMonths = Math.Max(1, months);
                var firstMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-(normalizedMonths - 1));
                var filter = new DashboardFilterDto
                {
                    TimePeriod = TimePeriodFilter.Custom,
                    FromDate = firstMonth,
                    ToDate = now.Date
                };

                return await GetBalanceTrendAsync(filter);
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<List<BalanceTrendDto>>(
                    ex,
                    OperationMessages.UnexpectedError,
                    "Error retrieving balance trend.");
            }
        }

        public async Task<OperationResult<List<BalanceTrendDto>>> GetBalanceTrendAsync(DashboardFilterDto filter)
        {
            try
            {
                filter ??= new DashboardFilterDto();
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var normalizedFilter = NormalizeOverviewFilter(filter, now);
                var (rangeStart, rangeEnd) = ResolveRangeLocal(normalizedFilter, now);

                var accountsResult = await _accountService.GetAccountsWithBalancesAsync();
                if (!accountsResult.Success)
                    return OperationResult<List<BalanceTrendDto>>.Fail(accountsResult.Message);
                if (accountsResult.Data is null)
                    return OperationResult<List<BalanceTrendDto>>.Fail("Accounts data is empty");

                var filteredAccounts = ApplyAccountFilter(accountsResult.Data, normalizedFilter.AccountIds);
                var currentNetWorth = BuildSummary(filteredAccounts).NetWorth;

                var movementEntries = await LoadTrendEntriesAsync(
                    rangeStart,
                    rangeEnd,
                    normalizedFilter.AccountIds,
                    normalizedFilter.TransactionFilter);

                var endNetWorth = currentNetWorth;
                if (rangeEnd.Date < now.Date)
                {
                    var postRangeEntries = await LoadTrendEntriesAsync(
                        rangeEnd.Date.AddDays(1),
                        now.Date,
                        normalizedFilter.AccountIds,
                        normalizedFilter.TransactionFilter);

                    endNetWorth -= postRangeEntries.Sum(x => x.Delta);
                }

                var startNetWorth = endNetWorth - movementEntries.Sum(x => x.Delta);
                var totalDays = (rangeEnd.Date - rangeStart.Date).TotalDays;
                var useMonthlyBuckets = totalDays > 120;

                var trend = useMonthlyBuckets
                    ? BuildMonthlyTrendFromMovements(rangeStart, rangeEnd, startNetWorth, movementEntries)
                    : BuildDailyTrendFromMovements(rangeStart, rangeEnd, startNetWorth, movementEntries);

                return OperationResult<List<BalanceTrendDto>>.Ok(trend, "Balance trend retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<List<BalanceTrendDto>>(
                    ex,
                    OperationMessages.UnexpectedError,
                    "Error retrieving balance trend.");
            }
        }

        public async Task<OperationResult<List<MonthlyComparisonDto>>> GetMonthlyComparisonAsync()
        {
            try
            {
                var currentMonth = await GetCashFlowAsync(TimePeriodFilter.ThisMonth);
                var previousMonth = await GetCashFlowAsync(TimePeriodFilter.LastMonth);

                if (!currentMonth.Success || !previousMonth.Success)
                    return OperationResult<List<MonthlyComparisonDto>>.Fail("Error retrieving monthly data");

                var comparisons = new List<MonthlyComparisonDto>();

                // Income comparison
                var incomeChange = currentMonth.Data.TotalIncome - previousMonth.Data.TotalIncome;
                var incomeChangePercentage = previousMonth.Data.TotalIncome != 0
                    ? (incomeChange / previousMonth.Data.TotalIncome) * 100 : 0;

                comparisons.Add(new MonthlyComparisonDto
                {
                    Metric = "Ingresos",
                    CurrentMonth = currentMonth.Data.TotalIncome,
                    PreviousMonth = previousMonth.Data.TotalIncome,
                    Change = incomeChange,
                    ChangePercentage = incomeChangePercentage,
                    IsImprovement = incomeChange >= 0,
                    Icon = "💰"
                });

                // Expenses comparison
                var expenseChange = currentMonth.Data.TotalExpenses - previousMonth.Data.TotalExpenses;
                var expenseChangePercentage = previousMonth.Data.TotalExpenses != 0
                    ? (expenseChange / previousMonth.Data.TotalExpenses) * 100 : 0;

                comparisons.Add(new MonthlyComparisonDto
                {
                    Metric = "Gastos",
                    CurrentMonth = currentMonth.Data.TotalExpenses,
                    PreviousMonth = previousMonth.Data.TotalExpenses,
                    Change = expenseChange,
                    ChangePercentage = expenseChangePercentage,
                    IsImprovement = expenseChange <= 0, // Lower expenses is better
                    Icon = "💸"
                });

                return OperationResult<List<MonthlyComparisonDto>>.Ok(comparisons, "Monthly comparison retrieved successfully");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<List<MonthlyComparisonDto>>(
                    ex,
                    OperationMessages.UnexpectedError,
                    ServiceMessages.DashboardMonthlyComparisonError);
            }
        }

        #region Private Helper Methods

        private List<AccountDto> ApplyAccountFilter(List<AccountDto> accounts, List<int> accountIds)
        {
            if (accountIds is null || accountIds.Count == 0)
                return accounts;

            return accounts
                .Where(a => accountIds.Contains(a.Id))
                .ToList();
        }

        private List<TransactionDto> ApplyTransactionFilter(List<TransactionDto> transactions, DashboardTransactionFilter transactionFilter)
        {
            return transactionFilter switch
            {
                DashboardTransactionFilter.Income => transactions.Where(t => t.IsIncome()).ToList(),
                DashboardTransactionFilter.Expense => transactions.Where(t => t.IsExpense()).ToList(),
                _ => transactions
            };
        }

        private static List<DashboardTransactionEntry> ApplyDashboardEntryFilter(
            List<DashboardTransactionEntry> entries,
            DashboardTransactionFilter transactionFilter)
        {
            return transactionFilter switch
            {
                DashboardTransactionFilter.Income => entries.Where(IsIncomeEntry).ToList(),
                DashboardTransactionFilter.Expense => entries.Where(IsExpenseEntry).ToList(),
                _ => entries
            };
        }

        private DashboardSummaryDto BuildSummary(List<AccountDto> accounts)
        {
            var assets = accounts
                .Where(a => a.CurrentBalance > 0)
                .Sum(a => a.CurrentBalance);

            var liabilities = Math.Abs(accounts
                .Where(a => a.CurrentBalance < 0)
                .Sum(a => Math.Min(a.CurrentBalance, 0)));

            var netWorth = assets - liabilities;

            return new DashboardSummaryDto
            {
                TotalAssets = assets,
                TotalLiabilities = liabilities,
                NetWorth = netWorth,
                MonthlyChange = 0m,
                MonthlyChangePercentage = 0m,
                IsPositiveChange = netWorth >= 0,
                Last6MonthsNetWorth = new List<decimal>()
            };
        }

        private CashFlowDto BuildCashFlow(List<TransactionDto> transactions, DateTime now)
        {
            var totalIncome = transactions
                .Where(t => t.IsIncome())
                .Sum(t => Math.Abs(t.Amount));

            var totalExpenses = transactions
                .Where(t => t.IsExpense())
                .Sum(t => Math.Abs(t.Amount));

            var netCashFlow = totalIncome - totalExpenses;
            var incomeByCategory = BuildCashFlowCategoryBreakdown(transactions, isIncome: true);
            var expenseByCategory = BuildCashFlowCategoryBreakdown(transactions, isIncome: false);
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            var daysElapsed = now.Day;
            var daysLeft = daysInMonth - daysElapsed;
            var dailyBurnRate = daysElapsed > 0 ? totalExpenses / daysElapsed : 0;
            var projectedMonthEnd = totalIncome - (dailyBurnRate * daysInMonth);

            return new CashFlowDto
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetCashFlow = netCashFlow,
                DailyBurnRate = dailyBurnRate,
                DaysLeftInMonth = daysLeft,
                ProjectedMonthEnd = projectedMonthEnd,
                DaysInMonth = daysInMonth,
                DaysElapsed = daysElapsed,
                IncomeByCategory = incomeByCategory,
                ExpenseByCategory = expenseByCategory
            };
        }

        private CashFlowDto BuildCashFlow(List<DashboardTransactionEntry> entries, DateTime now)
        {
            var totalIncome = entries
                .Where(IsIncomeEntry)
                .Sum(t => Math.Abs(t.Amount));

            var totalExpenses = entries
                .Where(IsExpenseEntry)
                .Sum(t => Math.Abs(t.Amount));

            var netCashFlow = totalIncome - totalExpenses;
            var incomeByCategory = BuildCashFlowCategoryBreakdown(entries, isIncome: true);
            var expenseByCategory = BuildCashFlowCategoryBreakdown(entries, isIncome: false);
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            var daysElapsed = now.Day;
            var daysLeft = daysInMonth - daysElapsed;
            var dailyBurnRate = daysElapsed > 0 ? totalExpenses / daysElapsed : 0;
            var projectedMonthEnd = totalIncome - (dailyBurnRate * daysInMonth);

            return new CashFlowDto
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetCashFlow = netCashFlow,
                DailyBurnRate = dailyBurnRate,
                DaysLeftInMonth = daysLeft,
                ProjectedMonthEnd = projectedMonthEnd,
                DaysInMonth = daysInMonth,
                DaysElapsed = daysElapsed,
                IncomeByCategory = incomeByCategory,
                ExpenseByCategory = expenseByCategory
            };
        }

        private static CashFlowDto BuildCashFlow(
            List<DashboardCategoryAggregateEntry> incomeCategories,
            List<DashboardCategoryAggregateEntry> expenseCategories,
            DateTime now)
        {
            var totalIncome = incomeCategories.Sum(x => x.Amount);
            var totalExpenses = expenseCategories.Sum(x => x.Amount);
            var netCashFlow = totalIncome - totalExpenses;
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            var daysElapsed = now.Day;
            var daysLeft = daysInMonth - daysElapsed;
            var dailyBurnRate = daysElapsed > 0 ? totalExpenses / daysElapsed : 0;
            var projectedMonthEnd = totalIncome - (dailyBurnRate * daysInMonth);

            return new CashFlowDto
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetCashFlow = netCashFlow,
                DailyBurnRate = dailyBurnRate,
                DaysLeftInMonth = daysLeft,
                ProjectedMonthEnd = projectedMonthEnd,
                DaysInMonth = daysInMonth,
                DaysElapsed = daysElapsed,
                IncomeByCategory = incomeCategories
                    .OrderByDescending(x => x.Amount)
                    .Select(x => x.MapToCashFlowCategory())
                    .ToList(),
                ExpenseByCategory = expenseCategories
                    .OrderByDescending(x => x.Amount)
                    .Select(x => x.MapToCashFlowCategory())
                    .ToList()
            };
        }

        private static List<CashFlowCategoryDto> BuildCashFlowCategoryBreakdown(
            List<TransactionDto> transactions,
            bool isIncome)
        {
            return transactions
                .Where(t => isIncome ? t.IsIncome() : t.IsExpense())
                .GroupBy(t => new
                {
                    t.CategoryId,
                    CategoryName = t.Category?.Name ?? DashboardConstants.UncategorizedNameNoAccent
                })
                .Select(g => DashboardMapper.MapToCashFlowCategory(
                    g.Key.CategoryId,
                    g.Key.CategoryName,
                    g.Sum(t => Math.Abs(t.Amount))))
                .OrderByDescending(x => x.Amount)
                .ToList();
        }

        private static List<CashFlowCategoryDto> BuildCashFlowCategoryBreakdown(
            List<DashboardTransactionEntry> entries,
            bool isIncome)
        {
            return entries
                .Where(t => isIncome ? IsIncomeEntry(t) : IsExpenseEntry(t))
                .GroupBy(t => new
                {
                    t.CategoryId,
                    CategoryName = t.CategoryName
                })
                .Select(g => DashboardMapper.MapToCashFlowCategory(
                    g.Key.CategoryId,
                    g.Key.CategoryName,
                    g.Sum(t => Math.Abs(t.Amount))))
                .OrderByDescending(x => x.Amount)
                .ToList();
        }

        private List<CategoryBreakdownDto> BuildCategoryBreakdown(List<TransactionDto> transactions)
        {
            var expenses = transactions
                .Where(t => t.IsExpense() && t.CategoryId > 0)
                .ToList();

            var totalExpenses = expenses.Sum(t => Math.Abs(t.Amount));

            return expenses
                .GroupBy(t => new
                {
                    t.CategoryId,
                    CategoryName = t.Category?.Name ?? DashboardConstants.UncategorizedName,
                    CategoryColor = t.Category?.Color ?? DashboardConstants.DefaultCategoryColor
                })
                .Select(g => DashboardMapper.MapToCategoryBreakdown(
                    g.Key.CategoryId,
                    g.Key.CategoryName,
                    g.Key.CategoryColor,
                    g.Sum(t => Math.Abs(t.Amount)),
                    totalExpenses,
                    g.Count()))
                .OrderByDescending(c => c.Amount)
                .Take(6)
                .ToList();
        }

        private static List<CategoryBreakdownDto> BuildCategoryBreakdown(List<DashboardTransactionEntry> entries)
        {
            var expenses = entries
                .Where(t => IsExpenseEntry(t) && t.CategoryId > 0)
                .ToList();

            var totalExpenses = expenses.Sum(t => Math.Abs(t.Amount));

            return expenses
                .GroupBy(t => new
                {
                    t.CategoryId,
                    t.CategoryName,
                    t.CategoryColor
                })
                .Select(g => DashboardMapper.MapToCategoryBreakdown(
                    g.Key.CategoryId,
                    g.Key.CategoryName,
                    g.Key.CategoryColor,
                    g.Sum(t => Math.Abs(t.Amount)),
                    totalExpenses,
                    g.Count()))
                .OrderByDescending(c => c.Amount)
                .Take(6)
                .ToList();
        }

        private static List<CategoryBreakdownDto> BuildCategoryBreakdown(List<DashboardCategoryAggregateEntry> aggregates)
        {
            var expenses = aggregates
                .Where(x => x.Amount > 0 && x.CategoryId > 0)
                .OrderByDescending(x => x.Amount)
                .ToList();

            var totalExpenses = expenses.Sum(x => x.Amount);

            return expenses
                .Select(x => x.MapToCategoryBreakdown(totalExpenses))
                .Take(6)
                .ToList();
        }

        private List<RecentTransactionDto> BuildRecentTransactions(List<TransactionDto> transactions, int count, DateTime now)
        {
            if (count <= 0 || transactions.Count == 0)
                return new List<RecentTransactionDto>();

            var latest = new PriorityQueue<TransactionDto, DateTime>();
            foreach (var transaction in transactions)
            {
                latest.Enqueue(transaction, transaction.Date);
                if (latest.Count > count)
                {
                    latest.Dequeue();
                }
            }

            return latest.UnorderedItems
                .Select(x => x.Element)
                .OrderByDescending(t => t.Date)
                .Select(t => t.MapToRecentTransaction(now, CalculateRelativeTime))
                .ToList();
        }

        private List<RecentTransactionDto> BuildRecentTransactions(List<DashboardTransactionEntry> entries, int count, DateTime now)
        {
            if (count <= 0 || entries.Count == 0)
                return new List<RecentTransactionDto>();

            var latest = new PriorityQueue<DashboardTransactionEntry, DateTime>();
            foreach (var entry in entries)
            {
                latest.Enqueue(entry, entry.Date);
                if (latest.Count > count)
                {
                    latest.Dequeue();
                }
            }

            return latest.UnorderedItems
                .Select(x => x.Element)
                .OrderByDescending(t => t.Date)
                .Select(t =>
                {
                    var localDate = _timeZoneService.ConvertFromUtc(t.Date);
                    var type = GetEntryType(t);
                    var relativeTime = CalculateRelativeTime(localDate, now);
                    return t.MapToRecentTransaction(localDate, type, relativeTime);
                })
                .ToList();
        }

        private async Task<List<DashboardTransactionEntry>> LoadDashboardEntriesAsync(DashboardFilterDto filter, DateTime now)
        {
            var (rangeStart, rangeEnd) = ResolveRangeLocal(filter, now);
            var fromUtc = _timeZoneService.ConvertToUtc(rangeStart.Date);
            var toUtc = _timeZoneService.ConvertToUtc(rangeEnd.Date.AddDays(1).AddTicks(-1));
            return await _transactionRepository.GetDashboardEntriesAsync(
                fromUtc,
                toUtc,
                filter.AccountIds ?? new List<int>());
        }

        private async Task<List<BalanceTrendMovementDto>> LoadTrendEntriesAsync(
            DateTime localStartDate,
            DateTime localEndDate,
            List<int> accountIds,
            DashboardTransactionFilter transactionFilter)
        {
            var fromUtc = _timeZoneService.ConvertToUtc(localStartDate.Date);
            var toUtc = _timeZoneService.ConvertToUtc(localEndDate.Date.AddDays(1).AddTicks(-1));

            var categoryType = transactionFilter switch
            {
                DashboardTransactionFilter.Income => CategoryTypeEnum.Income,
                DashboardTransactionFilter.Expense => CategoryTypeEnum.Expense,
                _ => (CategoryTypeEnum?)null
            };

            var entries = await _transactionRepository.GetTrendEntriesAsync(
                fromUtc,
                toUtc,
                accountIds ?? new List<int>(),
                categoryType);

            return (entries ?? new List<Domain.Entities.TransactionTrendEntry>())
                .Select(x => x.MapToMovementDto(_timeZoneService))
                .ToList();
        }

        private static List<BalanceTrendDto> BuildDailyTrendFromMovements(
            DateTime rangeStart,
            DateTime rangeEnd,
            decimal startNetWorth,
            List<BalanceTrendMovementDto> movements)
        {
            var movementByDay = movements
                .GroupBy(t => t.BucketDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Delta));

            var trend = new List<BalanceTrendDto>();
            var runningNetWorth = startNetWorth;
            for (var day = rangeStart.Date; day <= rangeEnd.Date; day = day.AddDays(1))
            {
                if (movementByDay.TryGetValue(day, out var delta))
                {
                    runningNetWorth += delta;
                }

                trend.Add(new BalanceTrendDto
                {
                    Date = day,
                    TotalBalance = runningNetWorth
                });
            }

            return trend;
        }

        private static List<BalanceTrendDto> BuildMonthlyTrendFromMovements(
            DateTime rangeStart,
            DateTime rangeEnd,
            decimal startNetWorth,
            List<BalanceTrendMovementDto> movements)
        {
            var movementByMonth = movements
                .GroupBy(t => new DateTime(t.BucketDate.Year, t.BucketDate.Month, 1))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Delta));

            var trend = new List<BalanceTrendDto>();
            var cursor = new DateTime(rangeStart.Year, rangeStart.Month, 1);
            var endMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);
            var runningNetWorth = startNetWorth;

            while (cursor <= endMonth)
            {
                if (movementByMonth.TryGetValue(cursor, out var delta))
                {
                    runningNetWorth += delta;
                }

                trend.Add(new BalanceTrendDto
                {
                    Date = cursor,
                    TotalBalance = runningNetWorth
                });

                cursor = cursor.AddMonths(1);
            }

            return trend;
        }

        private async Task<List<BalanceTrendDto>> BuildBalanceTrendAsync(
            List<AccountDto> accounts,
            List<TransactionDto> transactions,
            DashboardFilterDto filter,
            DateTime now)
        {
            if (accounts.Count == 0)
                return new List<BalanceTrendDto>();

            var (rangeStart, rangeEnd) = ResolveRangeLocal(filter, now);
            var currentNetWorth = BuildSummary(accounts).NetWorth;
            var rangeTransactions = transactions
                .Where(t => t.Date >= rangeStart && t.Date <= rangeEnd)
                .ToList();

            var endNetWorth = currentNetWorth;
            if (rangeEnd.Date < now.Date)
            {
                var postRangeResult = await _transactionService.GetFilteredAsync(new TransactionFilterDto
                {
                    TimePeriod = TimePeriodFilter.Custom,
                    FromDate = rangeEnd.Date.AddDays(1),
                    ToDate = now.Date,
                    AccountIds = filter.AccountIds,
                    SkipSorting = true
                });

                if (postRangeResult.Success && postRangeResult.Data is not null)
                {
                    var postRangeTransactions = ApplyTransactionFilter(
                        postRangeResult.Data.Transactions,
                        filter.TransactionFilter);

                    endNetWorth -= postRangeTransactions.Sum(GetSignedDelta);
                }
            }

            var startNetWorth = endNetWorth - rangeTransactions.Sum(GetSignedDelta);
            var totalDays = (rangeEnd.Date - rangeStart.Date).TotalDays;
            var useMonthlyBuckets = totalDays > 120;

            return useMonthlyBuckets
                ? BuildMonthlyTrend(rangeStart, rangeEnd, startNetWorth, rangeTransactions)
                : BuildDailyTrend(rangeStart, rangeEnd, startNetWorth, rangeTransactions);
        }

        private static List<BalanceTrendDto> BuildDailyTrend(
            DateTime rangeStart,
            DateTime rangeEnd,
            decimal startNetWorth,
            List<TransactionDto> transactions)
        {
            var movementByDay = transactions
                .GroupBy(t => t.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(GetSignedDelta));

            var trend = new List<BalanceTrendDto>();
            var runningNetWorth = startNetWorth;
            for (var day = rangeStart.Date; day <= rangeEnd.Date; day = day.AddDays(1))
            {
                if (movementByDay.TryGetValue(day, out var delta))
                {
                    runningNetWorth += delta;
                }

                trend.Add(new BalanceTrendDto
                {
                    Date = day,
                    TotalBalance = runningNetWorth
                });
            }

            return trend;
        }

        private static List<BalanceTrendDto> BuildMonthlyTrend(
            DateTime rangeStart,
            DateTime rangeEnd,
            decimal startNetWorth,
            List<TransactionDto> transactions)
        {
            var movementByMonth = transactions
                .GroupBy(t => new DateTime(t.Date.Year, t.Date.Month, 1))
                .ToDictionary(g => g.Key, g => g.Sum(GetSignedDelta));

            var trend = new List<BalanceTrendDto>();
            var cursor = new DateTime(rangeStart.Year, rangeStart.Month, 1);
            var endMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);
            var runningNetWorth = startNetWorth;

            while (cursor <= endMonth)
            {
                if (movementByMonth.TryGetValue(cursor, out var delta))
                {
                    runningNetWorth += delta;
                }

                trend.Add(new BalanceTrendDto
                {
                    Date = cursor,
                    TotalBalance = runningNetWorth
                });

                cursor = cursor.AddMonths(1);
            }

            return trend;
        }

        private static decimal GetSignedDelta(TransactionDto transaction)
        {
            if (transaction.IsIncome())
                return Math.Abs(transaction.Amount);

            if (transaction.IsExpense())
                return -Math.Abs(transaction.Amount);

            return 0m;
        }

        private static (DateTime start, DateTime end) ResolveRangeLocal(DashboardFilterDto filter, DateTime now)
        {
            var today = now.Date;

            if (filter.TimePeriod == TimePeriodFilter.Custom)
            {
                var start = filter.FromDate?.Date ?? today.AddDays(-30);
                var end = filter.ToDate?.Date ?? today;
                return (start, end);
            }

            return filter.TimePeriod switch
            {
                TimePeriodFilter.Last7Days => (today.AddDays(-7), today),
                TimePeriodFilter.Last30Days => (today.AddDays(-30), today),
                TimePeriodFilter.Last90Days => (today.AddDays(-90), today),
                TimePeriodFilter.LastMonth => (
                    new DateTime(today.AddMonths(-1).Year, today.AddMonths(-1).Month, 1),
                    new DateTime(
                        today.AddMonths(-1).Year,
                        today.AddMonths(-1).Month,
                        DateTime.DaysInMonth(today.AddMonths(-1).Year, today.AddMonths(-1).Month))),
                TimePeriodFilter.ThisMonth => (
                    new DateTime(today.Year, today.Month, 1),
                    today),
                TimePeriodFilter.ThisYear => (
                    new DateTime(today.Year, 1, 1),
                    today),
                _ => (today.AddDays(-30), today)
            };
        }

        private static DateTime ResolveBudgetReferenceDate(DashboardFilterDto? filter, DateTime now)
        {
            if (filter is null)
                return now;

            if (filter.TimePeriod == TimePeriodFilter.LastMonth)
                return now.AddMonths(-1);

            if (filter.TimePeriod == TimePeriodFilter.Custom)
            {
                if (filter.ToDate.HasValue)
                    return filter.ToDate.Value;

                if (filter.FromDate.HasValue)
                    return filter.FromDate.Value;
            }

            return now;
        }

        private async Task<DashboardBudgetSummaryDto> BuildBudgetSummaryAsync(DateTime now)
        {
            var budgetResult = await _budgetService.GetMonthlyWithUsageAsync(now.Year, now.Month);
            if (budgetResult is null || !budgetResult.Success || budgetResult.Data is null)
            {
                return new DashboardBudgetSummaryDto
                {
                    Year = now.Year,
                    Month = now.Month
                };
            }

            var expenseItems = budgetResult.Data
                .Where(x => x.CategoryType == Domain.Enums.Category.CategoryTypeEnum.Expense)
                .ToList();

            var overviewItems = GetBudgetOverviewItems(expenseItems);
            var totalBudgeted = overviewItems.Sum(x => x.BudgetAmount);
            var totalUsed = overviewItems.Sum(x => x.UsedAmount);
            var totalRemaining = totalBudgeted - totalUsed;
            var budgetedCategoryCount = overviewItems.Count(x => x.BudgetAmount > 0);
            var overBudgetCount = overviewItems.Count(x => x.IsOverBudget);
            var nearLimitCount = overviewItems.Count(x => x.IsNearLimit);
            var noBudgetActivityCount = expenseItems.Count(x => x.Budget.Amount <= 0 && Math.Abs(x.Used) > 0.01m);
            var progress = totalBudgeted <= 0
                ? 0
                : (int)Math.Clamp(Math.Round((double)(totalUsed / totalBudgeted) * 100), 0, 999);

            var topCategories = overviewItems
                .OrderByDescending(x => x.IsOverBudget)
                .ThenByDescending(x => x.IsNearLimit)
                .ThenByDescending(x => x.UsedAmount)
                .Take(5)
                .ToList();

            return new DashboardBudgetSummaryDto
            {
                Year = now.Year,
                Month = now.Month,
                TotalBudgeted = totalBudgeted,
                TotalUsed = totalUsed,
                TotalRemaining = totalRemaining,
                ProgressPercent = progress,
                BudgetedCategoryCount = budgetedCategoryCount,
                NearLimitCount = nearLimitCount,
                OverBudgetCount = overBudgetCount,
                NoBudgetActivityCount = noBudgetActivityCount,
                HealthLabel = GetBudgetHealthLabel(budgetedCategoryCount, nearLimitCount, overBudgetCount),
                TopCategories = topCategories
            };
        }

        private static string GetBudgetHealthLabel(int budgetedCategoryCount, int nearLimitCount, int overBudgetCount)
        {
            if (overBudgetCount > 0)
                return "Over budget";

            if (nearLimitCount > 0)
                return "At risk";

            if (budgetedCategoryCount > 0)
                return "Healthy";

            return "No budgets";
        }

        private static List<DashboardBudgetItemDto> GetBudgetOverviewItems(List<BudgetWithUsageDto> items)
        {
            var childrenByParent = items
                .Where(x => x.ParentCategoryId.HasValue)
                .GroupBy(x => x.ParentCategoryId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CategoryName).ToList());

            var roots = items
                .Where(x => x.ParentCategoryId is null)
                .OrderBy(x => x.CategoryName)
                .ToList();

            var result = new List<DashboardBudgetItemDto>();

            foreach (var root in roots)
                AddNode(root);

            return result;

            void AddNode(BudgetWithUsageDto item)
            {
                var children = childrenByParent.TryGetValue(item.Category.Id, out var childItems)
                    ? childItems
                    : new List<BudgetWithUsageDto>();

                if (item.Budget.Amount > 0)
                {
                    result.Add(MapBudgetOverviewItem(item));

                    if (!item.Budget.IncludeChildren)
                    {
                        foreach (var child in children)
                            AddNode(child);
                    }

                    return;
                }

                var budgetedChildren = children
                    .Where(x => x.Budget.Amount > 0)
                    .Select(MapBudgetOverviewItem)
                    .ToList();

                if (budgetedChildren.Count > 0)
                {
                    var groupedBudget = budgetedChildren.Sum(x => x.BudgetAmount);
                    var groupedUsed = budgetedChildren.Sum(x => x.UsedAmount);
                    var groupedProgress = groupedBudget <= 0
                        ? 0
                        : (int)Math.Clamp(Math.Round((double)(groupedUsed / groupedBudget) * 100), 0, 999);

                    result.Add(new DashboardBudgetItemDto
                    {
                        CategoryName = item.CategoryName,
                        CategoryColor = item.CategoryColor ?? DashboardConstants.DefaultCategoryColor,
                        BudgetAmount = groupedBudget,
                        UsedAmount = groupedUsed,
                        RemainingAmount = groupedBudget - groupedUsed,
                        ProgressPercent = groupedProgress,
                        IsOverBudget = groupedUsed > groupedBudget,
                        IsNearLimit = groupedBudget > 0 && groupedUsed <= groupedBudget && groupedProgress >= 60,
                        IsGroupOnly = true
                    });

                    return;
                }

                if (Math.Abs(item.Used) > 0.01m || children.Count == 0)
                    result.Add(MapBudgetOverviewItem(item));

                foreach (var child in children)
                    AddNode(child);
            }
        }

        private static DashboardBudgetItemDto MapBudgetOverviewItem(BudgetWithUsageDto item)
        {
            var usedAmount = Math.Abs(item.Used);
            var progress = item.Budget.Amount <= 0
                ? 0
                : (int)Math.Clamp(Math.Round((double)(usedAmount / item.Budget.Amount) * 100), 0, 999);

            return new DashboardBudgetItemDto
            {
                CategoryName = item.CategoryName,
                CategoryColor = item.CategoryColor ?? DashboardConstants.DefaultCategoryColor,
                BudgetAmount = item.Budget.Amount,
                UsedAmount = usedAmount,
                RemainingAmount = item.Budget.Amount - usedAmount,
                ProgressPercent = progress,
                IsOverBudget = item.Budget.Amount > 0 && usedAmount > item.Budget.Amount,
                IsNearLimit = item.Budget.Amount > 0 && usedAmount <= item.Budget.Amount && progress >= 60,
                IsGroupOnly = false
            };
        }

        private static List<DashboardSpendingTrendDto> BuildSpendingTrend(
            List<TransactionDto> transactions,
            DashboardFilterDto filter,
            DateTime now)
        {
            var (rangeStart, rangeEnd) = ResolveRangeLocal(filter, now);
            var expenses = transactions
                .Where(t => t.IsExpense())
                .Where(t => t.Date.Date >= rangeStart.Date && t.Date.Date <= rangeEnd.Date)
                .ToList();

            var totalDays = (rangeEnd.Date - rangeStart.Date).TotalDays;
            var useMonthlyBuckets = totalDays > 120;

            if (useMonthlyBuckets)
            {
                var grouped = expenses
                    .GroupBy(t => new DateTime(t.Date.Year, t.Date.Month, 1))
                    .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.Amount)));

                var cursor = new DateTime(rangeStart.Year, rangeStart.Month, 1);
                var endMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);
                var trend = new List<DashboardSpendingTrendDto>();

                while (cursor <= endMonth)
                {
                    grouped.TryGetValue(cursor, out var amount);
                    trend.Add(new DashboardSpendingTrendDto
                    {
                        Date = cursor,
                        Label = cursor.ToString("MMM yyyy"),
                        Amount = amount
                    });

                    cursor = cursor.AddMonths(1);
                }

                return trend;
            }

            var byDay = expenses
                .GroupBy(t => t.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.Amount)));

            var dailyTrend = new List<DashboardSpendingTrendDto>();
            for (var day = rangeStart.Date; day <= rangeEnd.Date; day = day.AddDays(1))
            {
                byDay.TryGetValue(day, out var amount);
                dailyTrend.Add(new DashboardSpendingTrendDto
                {
                    Date = day,
                    Label = day.ToString("dd MMM"),
                    Amount = amount
                });
            }

            return dailyTrend;
        }

        private List<DashboardSpendingTrendDto> BuildSpendingTrend(
            List<DashboardTransactionEntry> entries,
            DashboardFilterDto filter,
            DateTime now)
        {
            var (rangeStart, rangeEnd) = ResolveRangeLocal(filter, now);
            var expenses = entries
                .Where(IsExpenseEntry)
                .Select(t => new
                {
                    Date = _timeZoneService.ConvertFromUtc(t.Date),
                    Amount = t.Amount
                })
                .Where(t => t.Date.Date >= rangeStart.Date && t.Date.Date <= rangeEnd.Date)
                .ToList();

            var totalDays = (rangeEnd.Date - rangeStart.Date).TotalDays;
            var useMonthlyBuckets = totalDays > 120;

            if (useMonthlyBuckets)
            {
                var grouped = expenses
                    .GroupBy(t => new DateTime(t.Date.Year, t.Date.Month, 1))
                    .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.Amount)));

                var cursor = new DateTime(rangeStart.Year, rangeStart.Month, 1);
                var endMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);
                var trend = new List<DashboardSpendingTrendDto>();

                while (cursor <= endMonth)
                {
                    grouped.TryGetValue(cursor, out var amount);
                    trend.Add(new DashboardSpendingTrendDto
                    {
                        Date = cursor,
                        Label = cursor.ToString("MMM yyyy"),
                        Amount = amount
                    });

                    cursor = cursor.AddMonths(1);
                }

                return trend;
            }

            var byDay = expenses
                .GroupBy(t => t.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.Amount)));

            var dailyTrend = new List<DashboardSpendingTrendDto>();
            for (var day = rangeStart.Date; day <= rangeEnd.Date; day = day.AddDays(1))
            {
                byDay.TryGetValue(day, out var amount);
                dailyTrend.Add(new DashboardSpendingTrendDto
                {
                    Date = day,
                    Label = day.ToString("dd MMM"),
                    Amount = amount
                });
            }

            return dailyTrend;
        }

        private List<DashboardSpendingTrendDto> BuildSpendingTrend(
            List<DashboardAmountByDateEntry> entries,
            DashboardFilterDto filter,
            DateTime now)
        {
            var (rangeStart, rangeEnd) = ResolveRangeLocal(filter, now);
            var localEntries = entries
                .Select(x => new DashboardAmountByDateEntry
                {
                    Date = _timeZoneService.ConvertFromUtc(x.Date),
                    Amount = x.Amount
                })
                .Where(x => x.Date.Date >= rangeStart.Date && x.Date.Date <= rangeEnd.Date)
                .ToList();

            var totalDays = (rangeEnd.Date - rangeStart.Date).TotalDays;
            var useMonthlyBuckets = totalDays > 120;

            if (useMonthlyBuckets)
            {
                var grouped = localEntries
                    .GroupBy(t => new DateTime(t.Date.Year, t.Date.Month, 1))
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

                var cursor = new DateTime(rangeStart.Year, rangeStart.Month, 1);
                var endMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);
                var trend = new List<DashboardSpendingTrendDto>();

                while (cursor <= endMonth)
                {
                    grouped.TryGetValue(cursor, out var amount);
                    trend.Add(new DashboardSpendingTrendDto
                    {
                        Date = cursor,
                        Label = cursor.ToString("MMM yyyy"),
                        Amount = amount
                    });

                    cursor = cursor.AddMonths(1);
                }

                return trend;
            }

            var byDay = localEntries
                .GroupBy(t => t.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var dailyTrend = new List<DashboardSpendingTrendDto>();
            for (var day = rangeStart.Date; day <= rangeEnd.Date; day = day.AddDays(1))
            {
                byDay.TryGetValue(day, out var amount);
                dailyTrend.Add(new DashboardSpendingTrendDto
                {
                    Date = day,
                    Label = day.ToString("dd MMM"),
                    Amount = amount
                });
            }

            return dailyTrend;
        }

        private static bool IsIncomeEntry(DashboardTransactionEntry entry)
            => !IsTransferEntry(entry) && entry.CategoryType == CategoryTypeEnum.Income;

        private static bool IsExpenseEntry(DashboardTransactionEntry entry)
            => !IsTransferEntry(entry) && entry.CategoryType == CategoryTypeEnum.Expense;

        private static bool IsTransferEntry(DashboardTransactionEntry entry)
            => SystemCategoryCodes.IsTransfer(entry.SystemCategoryCode)
               || entry.CategoryType == CategoryTypeEnum.Transfer;

        private static TransactionTypeEnum GetEntryType(DashboardTransactionEntry entry)
        {
            if (IsTransferEntry(entry))
                return TransactionTypeEnum.Transfer;

            return entry.CategoryType == CategoryTypeEnum.Income
                ? TransactionTypeEnum.Income
                : TransactionTypeEnum.Expense;
        }

        private static DashboardFilterDto NormalizeOverviewFilter(DashboardFilterDto original, DateTime now)
        {
            var clone = new DashboardFilterDto
            {
                TimePeriod = original.TimePeriod,
                FromDate = original.FromDate,
                ToDate = original.ToDate,
                TransactionFilter = original.TransactionFilter,
                AccountIds = original.AccountIds?.ToList() ?? new List<int>()
            };

            var (rangeStart, rangeEnd) = ResolveRangeLocal(clone, now);
            var totalDays = (rangeEnd.Date - rangeStart.Date).TotalDays;

            if (clone.TimePeriod == TimePeriodFilter.AllTime || totalDays > DashboardMaxRangeDays)
            {
                clone.TimePeriod = TimePeriodFilter.Custom;
                clone.FromDate = now.Date.AddDays(-DashboardMaxRangeDays);
                clone.ToDate = now.Date;
            }

            return clone;
        }

        private string CalculateRelativeTime(DateTime transactionDate, DateTime now)
        {
            var diff = now - transactionDate;

            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} minutes ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hours ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} days ago";
            if (diff.TotalDays < 30)
                return $"{(int)(diff.TotalDays / 7)} weeks ago";

            return transactionDate.ToString("dd/MM/yyyy");
        }

        private async Task<OperationResult<T>> FailWithLoggedExceptionAsync<T>(Exception ex, string failMessage, string logContext)
        {
            _logger.LogError(ex, logContext);

            if (_errorLogService is not null)
            {
                await _errorLogService.LogExceptionAsync(ex, logContext);
            }

            return OperationResult<T>.Fail(failMessage);
        }

        #endregion
    }
}
