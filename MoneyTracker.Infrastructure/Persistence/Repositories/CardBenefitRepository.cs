using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

public class CardBenefitRepository : ICardBenefitRepository
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CardBenefitRepository> _logger;
    private readonly ITenantContext _tenantContext;

    public CardBenefitRepository(
        IServiceScopeFactory scopeFactory,
        ILogger<CardBenefitRepository> logger,
        ITenantContext tenantContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<List<CardBenefit>> GetAllAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            return await context.CardBenefits
                .Include(e => e.Account)
                .Include(e => e.Category)
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.Account!.Name)
                .ThenBy(e => e.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all card benefits");
            return new List<CardBenefit>();
        }
    }

    public async Task<List<CardBenefit>> GetByAccountAsync(int accountId)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            return await context.CardBenefits
                .Include(e => e.Account)
                .Include(e => e.Category)
                .Where(e => e.AccountId == accountId && !e.IsDeleted)
                .OrderBy(e => e.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting card benefits for account {AccountId}", accountId);
            return new List<CardBenefit>();
        }
    }

    public async Task<CardBenefit?> GetByIdAsync(int id)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            return await context.CardBenefits
                .Include(e => e.Account)
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting card benefit by ID: {Id}", id);
            return null;
        }
    }

    public async Task AddAsync(CardBenefit cardBenefit)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            context.CardBenefits.Add(cardBenefit);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding card benefit: {Name}", cardBenefit.Name);
            throw;
        }
    }

    public async Task UpdateAsync(CardBenefit cardBenefit)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            context.CardBenefits.Update(cardBenefit);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating card benefit: {Id}", cardBenefit.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            var entity = await context.CardBenefits.FindAsync(id);
            if (entity is null) return;

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting card benefit: {Id}", id);
            throw;
        }
    }
}
