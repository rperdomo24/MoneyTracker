using Microsoft.EntityFrameworkCore;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Infrastructure.Persistence
{
    public class MoneyTrackerDbContext : DbContext
    {
        public MoneyTrackerDbContext(DbContextOptions<MoneyTrackerDbContext> options) : base(options)
        {
        }

        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Income> Incomes => Set<Income>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
