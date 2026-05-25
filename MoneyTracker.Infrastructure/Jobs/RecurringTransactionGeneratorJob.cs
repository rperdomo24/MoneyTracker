using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Jobs
{
    public class RecurringTransactionGeneratorJob
    {
        private readonly IRecurringTransactionRepository _recurringRepo;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecurringTransactionGeneratorJob> _logger;

        public RecurringTransactionGeneratorJob(
            IRecurringTransactionRepository recurringRepo,
            IServiceScopeFactory scopeFactory,
            ILogger<RecurringTransactionGeneratorJob> logger)
        {
            _recurringRepo = recurringRepo;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task ExecuteAsync()
        {
            var today = DateTime.UtcNow;
            var dueItems = await _recurringRepo.GetDueAsync(today);

            if (dueItems.Count == 0)
            {
                _logger.LogInformation("RecurringTransactionGeneratorJob: no items due on {Date}", today.Date);
                return;
            }

            _logger.LogInformation("RecurringTransactionGeneratorJob: generating {Count} transactions", dueItems.Count);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<Persistence.MoneyTrackerDbContext>();

            foreach (var recurring in dueItems)
            {
                try
                {
                    var current = recurring.NextDate;
                    while (current.Date <= today.Date)
                    {
                        var transaction = new Transaction
                        {
                            TenantId = recurring.TenantId,
                            Name = recurring.Name,
                            Amount = recurring.Amount,
                            Date = current,
                            CategoryId = recurring.CategoryId,
                            AccountId = recurring.AccountId,
                            Description = recurring.Description,
                            PaymentMethod = recurring.PaymentMethod,
                            IsSystemGenerated = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        context.Transaction.Add(transaction);

                        // Update account balance
                        var account = await context.Accounts
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(a => a.Id == recurring.AccountId);

                        if (account is not null)
                            account.Balance += recurring.Amount;

                        current = RecurringTransactionMapper.AdvanceNextDate(current, recurring.Frequency);
                    }

                    recurring.NextDate = current;
                    recurring.LastGeneratedDate = today;
                    context.RecurringTransactions.Update(recurring);

                    await context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Generated recurring '{Name}' (TenantId={TenantId}), next={Next}",
                        recurring.Name, recurring.TenantId, recurring.NextDate.Date);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate recurring transaction {Id}", recurring.Id);
                }
            }
        }
    }
}
