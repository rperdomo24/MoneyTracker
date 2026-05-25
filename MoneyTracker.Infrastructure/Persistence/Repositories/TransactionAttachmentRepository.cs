using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

public class TransactionAttachmentRepository : ITransactionAttachmentRepository
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionAttachmentRepository> _logger;
    private readonly ITenantContext _tenantContext;

    public TransactionAttachmentRepository(
        IServiceScopeFactory scopeFactory,
        ILogger<TransactionAttachmentRepository> logger,
        ITenantContext tenantContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<List<TransactionAttachment>> GetByTransactionIdAsync(int transactionId)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            return await context.TransactionAttachments
                .Where(a => a.TransactionId == transactionId && !a.IsDeleted)
                .OrderBy(a => a.UploadedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachments for transaction {TransactionId}", transactionId);
            return new List<TransactionAttachment>();
        }
    }

    public async Task<TransactionAttachment?> GetByIdAsync(int id)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            return await context.TransactionAttachments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachment {Id}", id);
            return null;
        }
    }

    public async Task<TransactionAttachment> AddAsync(TransactionAttachment attachment)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

        context.TransactionAttachments.Add(attachment);
        await context.SaveChangesAsync();
        return attachment;
    }

    public async Task DeleteAsync(TransactionAttachment attachment)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

        context.TransactionAttachments.Attach(attachment);
        attachment.IsDeleted = true;
        attachment.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }
}
