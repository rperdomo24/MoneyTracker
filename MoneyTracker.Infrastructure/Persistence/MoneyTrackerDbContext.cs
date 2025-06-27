using Microsoft.EntityFrameworkCore;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Infrastructure.Persistence.Repositories.Seeds;

namespace MoneyTracker.Infrastructure.Persistence
{
    public class MoneyTrackerDbContext : DbContext
    {
        public MoneyTrackerDbContext(DbContextOptions<MoneyTrackerDbContext> options) : base(options)
        {
        }

        public DbSet<Transaction> Transaction => Set<Transaction>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>()
               .HasOne(c => c.Parent)
               .WithMany(c => c.Subcategories)
               .HasForeignKey(c => c.ParentId)
               .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Transaction>()
                .HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Transaction>()
                .HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Transaction>()
                .Property(e => e.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Account>()
                .Property(e => e.Type)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);

            CategorySeed.Seed(modelBuilder);
        }
    }
}
