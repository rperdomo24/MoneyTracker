using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Transaction;
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

            var failed = 0;
            foreach (var recurring in dueItems)
            {
                try
                {
                    await ProcessItemAsync(recurring, today);
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "Failed to generate recurring transaction {Id}", recurring.Id);
                }
            }

            // Items that succeeded already advanced NextDate, so a Hangfire retry only picks up the failed ones.
            if (failed > 0)
                throw new InvalidOperationException($"{failed} of {dueItems.Count} recurring transactions failed to generate.");
        }

        /// <summary>
        /// Generates every missed occurrence of one recurring item. Runs in its own DbContext and database
        /// transaction so a failure rolls back cleanly and never leaks tracked entities into other items.
        /// </summary>
        private async Task ProcessItemAsync(RecurringTransaction recurring, DateTime today)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<Persistence.MoneyTrackerDbContext>();
            await using var dbTransaction = await context.Database.BeginTransactionAsync();

            var originalNextDate = recurring.NextDate;
            var current = originalNextDate;
            var occurrencesThisRun = 0;

            var isExpense = recurring.Category?.Type != CategoryTypeEnum.Income;
            var signedAmount = isExpense ? Math.Abs(recurring.Amount) * -1 : Math.Abs(recurring.Amount);

            while (current.Date <= today.Date)
            {
                if (recurring.TotalOccurrences.HasValue &&
                    recurring.OccurrencesGenerated + occurrencesThisRun >= recurring.TotalOccurrences.Value)
                    break;
                if (recurring.EndDate.HasValue && current.Date > recurring.EndDate.Value.Date)
                    break;

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

                occurrencesThisRun++;
                current = RecurringTransactionMapper.AdvanceNextDate(current, recurring.Frequency);
            }

            if (occurrencesThisRun == 0)
            {
                // Limit already reached: complete it so the item stops showing up as due every day.
                var completed = await context.RecurringTransactions
                    .IgnoreQueryFilters()
                    .Where(r => r.Id == recurring.Id && r.NextDate == originalNextDate && r.Status == RecurringStatus.Active)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, RecurringStatus.Completed));

                await dbTransaction.CommitAsync();
                _logger.LogInformation("Recurring '{Name}' (Id={Id}) limit reached — completed={Completed}",
                    recurring.Name, recurring.Id, completed > 0);
                return;
            }

            // Only Added entities with TenantId set — passes tenant enforcement
            await context.SaveChangesAsync();

            var newOccurrencesGenerated = recurring.OccurrencesGenerated + occurrencesThisRun;
            var limitReached =
                (recurring.TotalOccurrences.HasValue && newOccurrencesGenerated >= recurring.TotalOccurrences.Value) ||
                (recurring.EndDate.HasValue && current.Date > recurring.EndDate.Value.Date);

            // Guarded by the NextDate we read and by Active status: if another run already processed this item,
            // or the user paused it mid-run, nothing matches and we roll back instead of duplicating transactions.
            var now = DateTime.UtcNow;
            var guarded = context.RecurringTransactions
                .IgnoreQueryFilters()
                .Where(r => r.Id == recurring.Id && r.NextDate == originalNextDate && r.Status == RecurringStatus.Active);

            var advanced = limitReached
                ? await guarded.ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.NextDate, current)
                    .SetProperty(x => x.LastGeneratedDate, now)
                    .SetProperty(x => x.OccurrencesGenerated, newOccurrencesGenerated)
                    .SetProperty(x => x.Status, RecurringStatus.Completed))
                : await guarded.ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.NextDate, current)
                    .SetProperty(x => x.LastGeneratedDate, now)
                    .SetProperty(x => x.OccurrencesGenerated, newOccurrencesGenerated));

            if (advanced == 0)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogWarning("Recurring '{Name}' (Id={Id}) was already processed by another run — skipped",
                    recurring.Name, recurring.Id);
                return;
            }

            var newBalance = await context.Transaction
                .IgnoreQueryFilters()
                .Where(t => t.AccountId == recurring.AccountId && !t.IsDeleted)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            await context.Accounts
                .IgnoreQueryFilters()
                .Where(a => a.Id == recurring.AccountId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Balance, newBalance));

            await dbTransaction.CommitAsync();

            _logger.LogInformation("Generated recurring '{Name}' (TenantId={TenantId}), occurrences={Generated}/{Total}, next={Next}",
                recurring.Name, recurring.TenantId, newOccurrencesGenerated,
                recurring.TotalOccurrences.HasValue ? recurring.TotalOccurrences.Value.ToString() : "∞",
                current.Date);
        }
    }
}
