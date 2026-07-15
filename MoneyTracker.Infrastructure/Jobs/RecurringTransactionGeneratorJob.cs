using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Jobs
{
    public class RecurringTransactionGeneratorJob
    {
        private readonly IRecurringTransactionRepository _recurringRepo;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITimeZoneService _timeZoneService;
        private readonly ILogger<RecurringTransactionGeneratorJob> _logger;

        public RecurringTransactionGeneratorJob(
            IRecurringTransactionRepository recurringRepo,
            IServiceScopeFactory scopeFactory,
            ITimeZoneService timeZoneService,
            ILogger<RecurringTransactionGeneratorJob> logger)
        {
            _recurringRepo = recurringRepo;
            _scopeFactory = scopeFactory;
            _timeZoneService = timeZoneService;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task ExecuteAsync()
        {
            var today = _timeZoneService.GetLocalTimeInConfiguredTimeZone();
            var dueItems = await _recurringRepo.GetDueAsync(today);

            if (dueItems.Count == 0)
            {
                _logger.LogInformation("RecurringTransactionGeneratorJob: no items due on {Date}", today.Date);
                return;
            }

            _logger.LogInformation("RecurringTransactionGeneratorJob: {Count} items due", dueItems.Count);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<Persistence.MoneyTrackerDbContext>();

            foreach (var recurring in dueItems)
            {
                try
                {
                    var current = recurring.NextDate;
                    decimal totalAmount = 0;

                    var isExpense = recurring.Category?.Type != CategoryTypeEnum.Income;
                    var signedAmount = isExpense ? Math.Abs(recurring.Amount) * -1 : Math.Abs(recurring.Amount);

                    while (current.Date <= today.Date)
                    {
                        context.Transaction.Add(new Transaction
                        {
                            TenantId = recurring.TenantId,
                            Name = recurring.Name,
                            Amount = signedAmount,
                            Date = current,
                            CategoryId = recurring.CategoryId,
                            AccountId = recurring.AccountId,
                            Description = recurring.Description,
                            PaymentMethod = recurring.PaymentMethod,
                            IsSystemGenerated = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });

                        totalAmount += recurring.Amount;
                        current = RecurringTransactionMapper.AdvanceNextDate(current, recurring.Frequency);
                    }

                    // Only Added entities with TenantId set — passes tenant enforcement
                    await context.SaveChangesAsync();

                    var newBalance = await context.Transaction
                        .IgnoreQueryFilters()
                        .Where(t => t.AccountId == recurring.AccountId && !t.IsDeleted)
                        .SumAsync(t => (decimal?)t.Amount) ?? 0m;

                    await context.Accounts
                        .IgnoreQueryFilters()
                        .Where(a => a.Id == recurring.AccountId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.Balance, newBalance));

                    var now = DateTime.UtcNow;
                    await context.RecurringTransactions
                        .IgnoreQueryFilters()
                        .Where(r => r.Id == recurring.Id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.NextDate, current)
                            .SetProperty(x => x.LastGeneratedDate, now));

                    _logger.LogInformation("Generated recurring '{Name}' (TenantId={TenantId}), next={Next}",
                        recurring.Name, recurring.TenantId, current.Date);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate recurring transaction {Id}", recurring.Id);
                }
            }
        }
    }
}
