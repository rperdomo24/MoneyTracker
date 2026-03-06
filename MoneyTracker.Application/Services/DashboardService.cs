using MoneyTracker.Application.Common;
using MoneyTracker.Application.Common.Extensions;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Dashboard;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Filters;

namespace MoneyTracker.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IAccountService _accountService;
        private readonly ITransactionService _transactionService;
        private readonly ITimeZoneService _timeZoneService;

        public DashboardService(
            IAccountService accountService,
            ITransactionService transactionService,
            ITimeZoneService timeZoneService)
        {
            _accountService = accountService;
            _transactionService = transactionService;
            _timeZoneService = timeZoneService;
        }

        public async Task<OperationResult<DashboardOverviewDto>> GetOverviewAsync(int recentTransactionsCount = 10, int balanceTrendMonths = 6)
        {
            try
            {
                var accountsResult = await _accountService.GetAccountsWithBalancesAsync();
                if (!accountsResult.Success)
                    return OperationResult<DashboardOverviewDto>.Fail(accountsResult.Message);
                if (accountsResult.Data is null)
                    return OperationResult<DashboardOverviewDto>.Fail("Accounts data is empty");

                var thisMonthResult = await _transactionService.GetFilteredAsync(new TransactionFilterDto
                {
                    TimePeriod = TimePeriodFilter.ThisMonth
                });

                if (!thisMonthResult.Success)
                    return OperationResult<DashboardOverviewDto>.Fail(thisMonthResult.Message);
                if (thisMonthResult.Data is null)
                    return OperationResult<DashboardOverviewDto>.Fail("Current month transactions data is empty");

                var lastMonthResult = await _transactionService.GetFilteredAsync(new TransactionFilterDto
                {
                    TimePeriod = TimePeriodFilter.LastMonth
                });

                if (!lastMonthResult.Success)
                    return OperationResult<DashboardOverviewDto>.Fail(lastMonthResult.Message);
                if (lastMonthResult.Data is null)
                    return OperationResult<DashboardOverviewDto>.Fail("Previous month transactions data is empty");

                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var accounts = accountsResult.Data;
                var thisMonthTransactions = thisMonthResult.Data.Transactions;
                var lastMonthTransactions = lastMonthResult.Data.Transactions;
                var recentSource = thisMonthTransactions
                    .Concat(lastMonthTransactions)
                    .GroupBy(t => t.Id)
                    .Select(g => g.First())
                    .ToList();

                var overview = new DashboardOverviewDto
                {
                    Summary = BuildSummary(accounts),
                    CashFlow = BuildCashFlow(thisMonthTransactions, now),
                    CategoryBreakdown = BuildCategoryBreakdown(thisMonthTransactions),
                    RecentTransactions = BuildRecentTransactions(recentSource, recentTransactionsCount, now),
                    BalanceTrend = BuildBalanceTrend(accounts, balanceTrendMonths, now)
                };

                return OperationResult<DashboardOverviewDto>.Ok(overview, "Dashboard overview retrieved successfully");
            }
            catch (Exception ex)
            {
                return OperationResult<DashboardOverviewDto>.Fail($"Error retrieving dashboard overview: {ex.Message}");
            }
        }

        public async Task<OperationResult<DashboardSummaryDto>> GetSummaryAsync()
        {
            try
            {
                var accountsResult = await _accountService.GetAccountsWithBalancesAsync();
                if (!accountsResult.Success)
                    return OperationResult<DashboardSummaryDto>.Fail(accountsResult.Message);

                var accounts = accountsResult.Data;

                var assets = accounts
                    .Where(a => a.Type != AccountType.Credit && a.CurrentBalance >= 0)
                    .Sum(a => a.CurrentBalance);

                var liabilities = Math.Abs(accounts
                    .Where(a => a.Type == AccountType.Credit || a.CurrentBalance < 0)
                    .Sum(a => Math.Min(a.CurrentBalance, 0)));

                var netWorth = assets - liabilities;

                // Simplified calculation - no recursive calls
                var monthlyChange = 0m; // You can implement this separately later
                var monthlyChangePercentage = 0m;

                var summary = new DashboardSummaryDto
                {
                    TotalAssets = assets,
                    TotalLiabilities = liabilities,
                    NetWorth = netWorth,
                    MonthlyChange = monthlyChange,
                    MonthlyChangePercentage = monthlyChangePercentage,
                    IsPositiveChange = monthlyChange >= 0,
                    Last6MonthsNetWorth = new List<decimal>() // Empty for now
                };

                return OperationResult<DashboardSummaryDto>.Ok(summary, "Dashboard summary retrieved successfully");
            }
            catch (Exception ex)
            {
                return OperationResult<DashboardSummaryDto>.Fail($"Error retrieving dashboard summary: {ex.Message}");
            }
        }

        public async Task<OperationResult<CashFlowDto>> GetCashFlowAsync(TimePeriodFilter period = TimePeriodFilter.ThisMonth)
        {
            try
            {
                var filter = new TransactionFilterDto
                {
                    TimePeriod = period
                };

                var transactionsResult = await _transactionService.GetFilteredAsync(filter);
                if (!transactionsResult.Success)
                    return OperationResult<CashFlowDto>.Fail(transactionsResult.Message);

                var transactions = transactionsResult.Data.Transactions;

                var totalIncome = transactions
                    .Where(t => t.IsIncome())
                    .Sum(t => t.Amount);

                var totalExpenses = transactions
                    .Where(t => t.IsExpense())
                    .Sum(t => t.Amount);

                var netCashFlow = totalIncome - totalExpenses;

                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                var daysElapsed = now.Day;
                var daysLeft = daysInMonth - daysElapsed;

                var dailyBurnRate = daysElapsed > 0 ? totalExpenses / daysElapsed : 0;
                var projectedMonthEnd = totalIncome - (dailyBurnRate * daysInMonth);

                var cashFlow = new CashFlowDto
                {
                    TotalIncome = totalIncome,
                    TotalExpenses = totalExpenses,
                    NetCashFlow = netCashFlow,
                    DailyBurnRate = dailyBurnRate,
                    DaysLeftInMonth = daysLeft,
                    ProjectedMonthEnd = projectedMonthEnd,
                    DaysInMonth = daysInMonth,
                    DaysElapsed = daysElapsed
                };

                return OperationResult<CashFlowDto>.Ok(cashFlow, "Cash flow retrieved successfully");
            }
            catch (Exception ex)
            {
                return OperationResult<CashFlowDto>.Fail($"Error retrieving cash flow: {ex.Message}");
            }
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
                return OperationResult<List<FinancialAlertDto>>.Fail($"Error retrieving alerts: {ex.Message}");
            }
        }

        public async Task<OperationResult<List<CategoryBreakdownDto>>> GetCategoryBreakdownAsync(TimePeriodFilter period = TimePeriodFilter.ThisMonth)
        {
            try
            {
                var filter = new TransactionFilterDto
                {
                    TimePeriod = period
                };

                var transactionsResult = await _transactionService.GetFilteredAsync(filter);
                if (!transactionsResult.Success)
                    return OperationResult<List<CategoryBreakdownDto>>.Fail(transactionsResult.Message);

                var transactions = transactionsResult.Data.Transactions
                    .Where(t => t.IsExpense() && t.CategoryId > 0)
                    .ToList();

                var totalExpenses = transactions.Sum(t => t.Amount);

                var categoryBreakdown = transactions
                    .GroupBy(t => new {
                        t.CategoryId,
                        CategoryName = t.Category.Name ?? "Sin categoría",
                        CategoryColor = t.Category.Color ?? "#9e9e9e"
                    })
                    .Select(g => new CategoryBreakdownDto
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.CategoryName,
                        CategoryColor = g.Key.CategoryColor,
                        Amount = g.Sum(t => t.Amount),
                        Percentage = totalExpenses > 0 ? (g.Sum(t => t.Amount) / totalExpenses) * 100 : 0,
                        TransactionCount = g.Count(),
                        AverageTransaction = g.Count() > 0 ? g.Sum(t => t.Amount) / g.Count() : 0
                    })
                    .OrderByDescending(c => c.Amount)
                    .Take(6)
                    .ToList();

                return OperationResult<List<CategoryBreakdownDto>>.Ok(categoryBreakdown, "Category breakdown retrieved successfully");
            }
            catch (Exception ex)
            {
                return OperationResult<List<CategoryBreakdownDto>>.Fail($"Error retrieving category breakdown: {ex.Message}");
            }
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
                    TimePeriod = TimePeriodFilter.ThisMonth
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
                    var totalVolume = accountTransactions.Sum(t => t.Amount);
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
                        Color = account.Color ?? "#9e9e9e",
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
                return OperationResult<List<AccountActivityDto>>.Fail($"Error retrieving account activity: {ex.Message}");
            }
        }

        public async Task<OperationResult<List<RecentTransactionDto>>> GetRecentTransactionsAsync(int count = 10)
        {
            try
            {
                // OPTIMIZED: Get recent transactions with a filter instead of all transactions
                var filter = new TransactionFilterDto
                {
                    TimePeriod = TimePeriodFilter.LastMonth // Get last month to ensure we have enough data
                };

                var transactionsResult = await _transactionService.GetFilteredAsync(filter);
                if (!transactionsResult.Success)
                    return OperationResult<List<RecentTransactionDto>>.Fail(transactionsResult.Message);

                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);

                var recentTransactions = transactionsResult.Data.Transactions
                    .OrderByDescending(t => t.Date)
                    .Take(count)
                    .Select(t => new RecentTransactionDto
                    {
                        Id = t.Id,
                        Description = t.Description,
                        Amount = t.Amount,
                        Date = t.Date,
                        AccountName = t.Account?.Name ?? "Cuenta desconocida",
                        CategoryName = t.Category?.Name ?? "Sin categoría",
                        CategoryColor = t.Category?.Color ?? "#9e9e9e",
                        Type = t.TransactionType,
                        RelativeTime = CalculateRelativeTime(t.Date, now)
                    })
                    .ToList();

                return OperationResult<List<RecentTransactionDto>>.Ok(recentTransactions, "Recent transactions retrieved successfully");
            }
            catch (Exception ex)
            {
                return OperationResult<List<RecentTransactionDto>>.Fail($"Error retrieving recent transactions: {ex.Message}");
            }
        }

        public async Task<OperationResult<List<BalanceTrendDto>>> GetBalanceTrendAsync(int months = 6)
        {
            try
            {
                // SIMPLIFIED: Return mock data for now to avoid complex calculations
                var now = _timeZoneService.ConvertFromUtc(DateTime.UtcNow);
                var trends = new List<BalanceTrendDto>();

                // Get current balances once
                var accountsResult = await _accountService.GetAccountsWithBalancesAsync();
                if (!accountsResult.Success)
                    return OperationResult<List<BalanceTrendDto>>.Fail(accountsResult.Message);

                var accounts = accountsResult.Data;
                var currentChecking = accounts.Where(a => a.Type == AccountType.Checking).Sum(a => a.CurrentBalance);
                var currentSavings = accounts.Where(a => a.Type == AccountType.Savings).Sum(a => a.CurrentBalance);
                var currentCredit = accounts.Where(a => a.Type == AccountType.Credit).Sum(a => a.CurrentBalance);
                var currentInvestment = accounts.Where(a => a.Type == AccountType.Investment).Sum(a => a.CurrentBalance);
                var currentCash = accounts.Where(a => a.Type == AccountType.Cash).Sum(a => a.CurrentBalance);

                // Generate simplified trend data (mock variation for demo)
                for (int i = months - 1; i >= 0; i--)
                {
                    var date = now.AddMonths(-i);
                    var firstOfMonth = new DateTime(date.Year, date.Month, 1);

                    // Simple mock variation (in real implementation, calculate from historical data)
                    var factor = 1 + (i * 0.02m); // Small variation for demo

                    trends.Add(new BalanceTrendDto
                    {
                        Date = firstOfMonth,
                        CheckingBalance = currentChecking / factor,
                        SavingsBalance = currentSavings / factor,
                        CreditBalance = currentCredit / factor,
                        InvestmentBalance = currentInvestment / factor,
                        CashBalance = currentCash / factor,
                        TotalBalance = (currentChecking + currentSavings + currentInvestment + currentCash + currentCredit) / factor
                    });
                }

                return OperationResult<List<BalanceTrendDto>>.Ok(trends, "Balance trend retrieved successfully");
            }
            catch (Exception ex)
            {
                return OperationResult<List<BalanceTrendDto>>.Fail($"Error retrieving balance trend: {ex.Message}");
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
                return OperationResult<List<MonthlyComparisonDto>>.Fail($"Error retrieving monthly comparison: {ex.Message}");
            }
        }

        #region Private Helper Methods

        private DashboardSummaryDto BuildSummary(List<AccountDto> accounts)
        {
            var assets = accounts
                .Where(a => a.Type != AccountType.Credit && a.CurrentBalance >= 0)
                .Sum(a => a.CurrentBalance);

            var liabilities = Math.Abs(accounts
                .Where(a => a.Type == AccountType.Credit || a.CurrentBalance < 0)
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
                .Sum(t => t.Amount);

            var totalExpenses = transactions
                .Where(t => t.IsExpense())
                .Sum(t => t.Amount);

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
                DaysElapsed = daysElapsed
            };
        }

        private List<CategoryBreakdownDto> BuildCategoryBreakdown(List<TransactionDto> transactions)
        {
            var expenses = transactions
                .Where(t => t.IsExpense() && t.CategoryId > 0)
                .ToList();

            var totalExpenses = expenses.Sum(t => t.Amount);

            return expenses
                .GroupBy(t => new
                {
                    t.CategoryId,
                    CategoryName = t.Category?.Name ?? "Sin categoría",
                    CategoryColor = t.Category?.Color ?? "#9e9e9e"
                })
                .Select(g => new CategoryBreakdownDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    CategoryColor = g.Key.CategoryColor,
                    Amount = g.Sum(t => t.Amount),
                    Percentage = totalExpenses > 0 ? (g.Sum(t => t.Amount) / totalExpenses) * 100 : 0,
                    TransactionCount = g.Count(),
                    AverageTransaction = g.Count() > 0 ? g.Sum(t => t.Amount) / g.Count() : 0
                })
                .OrderByDescending(c => c.Amount)
                .Take(6)
                .ToList();
        }

        private List<RecentTransactionDto> BuildRecentTransactions(List<TransactionDto> transactions, int count, DateTime now)
        {
            return transactions
                .OrderByDescending(t => t.Date)
                .Take(count)
                .Select(t => new RecentTransactionDto
                {
                    Id = t.Id,
                    Description = t.Description,
                    Amount = t.Amount,
                    Date = t.Date,
                    AccountName = t.Account?.Name ?? "Cuenta desconocida",
                    CategoryName = t.Category?.Name ?? "Sin categoría",
                    CategoryColor = t.Category?.Color ?? "#9e9e9e",
                    Type = t.TransactionType,
                    RelativeTime = CalculateRelativeTime(t.Date, now)
                })
                .ToList();
        }

        private List<BalanceTrendDto> BuildBalanceTrend(List<AccountDto> accounts, int months, DateTime now)
        {
            var trends = new List<BalanceTrendDto>();
            var currentChecking = accounts.Where(a => a.Type == AccountType.Checking).Sum(a => a.CurrentBalance);
            var currentSavings = accounts.Where(a => a.Type == AccountType.Savings).Sum(a => a.CurrentBalance);
            var currentCredit = accounts.Where(a => a.Type == AccountType.Credit).Sum(a => a.CurrentBalance);
            var currentInvestment = accounts.Where(a => a.Type == AccountType.Investment).Sum(a => a.CurrentBalance);
            var currentCash = accounts.Where(a => a.Type == AccountType.Cash).Sum(a => a.CurrentBalance);

            for (int i = months - 1; i >= 0; i--)
            {
                var date = now.AddMonths(-i);
                var firstOfMonth = new DateTime(date.Year, date.Month, 1);
                var factor = 1 + (i * 0.02m);

                trends.Add(new BalanceTrendDto
                {
                    Date = firstOfMonth,
                    CheckingBalance = currentChecking / factor,
                    SavingsBalance = currentSavings / factor,
                    CreditBalance = currentCredit / factor,
                    InvestmentBalance = currentInvestment / factor,
                    CashBalance = currentCash / factor,
                    TotalBalance = (currentChecking + currentSavings + currentInvestment + currentCash + currentCredit) / factor
                });
            }

            return trends;
        }

        private string CalculateRelativeTime(DateTime transactionDate, DateTime now)
        {
            var diff = now - transactionDate;

            if (diff.TotalMinutes < 60)
                return $"Hace {(int)diff.TotalMinutes} minutos";
            if (diff.TotalHours < 24)
                return $"Hace {(int)diff.TotalHours} horas";
            if (diff.TotalDays < 7)
                return $"Hace {(int)diff.TotalDays} días";
            if (diff.TotalDays < 30)
                return $"Hace {(int)(diff.TotalDays / 7)} semanas";

            return transactionDate.ToString("dd/MM/yyyy");
        }

        #endregion
    }
}
