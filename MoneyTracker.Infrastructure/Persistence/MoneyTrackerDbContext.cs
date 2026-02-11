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
        public DbSet<Budget> Budgets => Set<Budget>();


        public DbSet<TransactionAttachment> TransactionAttachments => Set<TransactionAttachment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>()
               .HasOne(c => c.Parent)
               .WithMany(c => c.Children)
               .HasForeignKey(c => c.ParentId)
               .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Transaction>()
                .HasOne(e => e.Category)
                .WithMany(e => e.Transactions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Transaction>()
                .HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Account>()
                .Property(e => e.Type)
                .HasConversion<string>();

            modelBuilder.Entity<TransactionAttachment>()
             .HasOne(cf => cf.Transaction)
             .WithMany(c => c.Attachments)
             .HasForeignKey(cf => cf.TransactionId)
             .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Budget>()
             .HasOne(b => b.Category)
             .WithMany(c => c.Budgets)
             .HasForeignKey(b => b.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Budget>()
            .HasIndex(b => new { b.CategoryId, b.Year, b.Month })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");


            modelBuilder.Entity<Budget>()
                .Property(b => b.Amount)
                .HasColumnType("numeric(12,2)");

            modelBuilder.Entity<Budget>()
                .Property(b => b.Month)
                .IsRequired();

            modelBuilder.Entity<Budget>()
                .Property(b => b.Year)
                .IsRequired();

            modelBuilder.Entity<Budget>()
            .Property(b => b.CreatedAt)
            .HasDefaultValueSql("now()");

            modelBuilder.Entity<Budget>()
                .Property(b => b.UpdatedAt)
                .HasDefaultValueSql("now()");

            base.OnModelCreating(modelBuilder);

            CategorySeed.Seed(modelBuilder);
        }
    }
}
