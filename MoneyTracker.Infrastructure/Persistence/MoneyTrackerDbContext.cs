using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Infrastructure.Persistence
{
    public class MoneyTrackerDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        private readonly ITenantContext _tenantContext;

        private Guid? CurrentTenantId => _tenantContext.TenantId;

        public MoneyTrackerDbContext(
            DbContextOptions<MoneyTrackerDbContext> options,
            ITenantContext tenantContext) : base(options)
        {
            _tenantContext = tenantContext;
        }

        public DbSet<Transaction> Transaction => Set<Transaction>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Budget> Budgets => Set<Budget>();
        public DbSet<TransactionAttachment> TransactionAttachments => Set<TransactionAttachment>();
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<UserAvatar> UserAvatars => Set<UserAvatar>();
        public DbSet<UserVerificationCode> UserVerificationCodes => Set<UserVerificationCode>();
        public DbSet<UserVerificationCodeAudit> UserVerificationCodeAudits => Set<UserVerificationCodeAudit>();
        public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
        public DbSet<AuthAuditLog> AuthAuditLogs => Set<AuthAuditLog>();
        public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();
        public DbSet<CardBenefit> CardBenefits => Set<CardBenefit>();
        public DbSet<Merchant> Merchants => Set<Merchant>();
        public DbSet<TransactionRule> TransactionRules => Set<TransactionRule>();
        public DbSet<TransactionRuleCondition> TransactionRuleConditions => Set<TransactionRuleCondition>();
        public DbSet<TransactionRuleAction> TransactionRuleActions => Set<TransactionRuleAction>();
        public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
        public DbSet<SavingsContribution> SavingsContributions => Set<SavingsContribution>();
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
        public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
        public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
        public DbSet<AppNotification> AppNotifications => Set<AppNotification>();
        public DbSet<AiReportCache> AiReportCaches => Set<AiReportCache>();
        public DbSet<UserReportPreference> UserReportPreferences => Set<UserReportPreference>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique();

            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.OwnerUserId)
                .IsUnique();

            modelBuilder.Entity<UserAvatar>()
                .HasIndex(a => a.TenantId);

            modelBuilder.Entity<UserAvatar>()
                .HasOne<ApplicationUser>()
                .WithOne(u => u.Avatar)
                .HasForeignKey<UserAvatar>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Account>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<Category>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<Category>()
                .HasIndex(e => new { e.TenantId, e.SystemCategoryCode })
                .IsUnique()
                .HasFilter("\"SystemCategoryCode\" IS NOT NULL");

            modelBuilder.Entity<Transaction>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<Budget>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<TransactionAttachment>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<UserAvatar>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value);

            modelBuilder.Entity<UserVerificationCode>()
                .HasIndex(e => new { e.TenantId, e.UserId, e.Purpose, e.CreatedAtUtc });

            modelBuilder.Entity<UserVerificationCodeAudit>()
                .HasIndex(e => new { e.TenantId, e.UserId, e.Purpose, e.EventAtUtc });

            modelBuilder.Entity<UserInvitation>()
                .HasIndex(e => e.NormalizedEmail)
                .IsUnique();

            modelBuilder.Entity<UserInvitation>()
                .HasIndex(e => e.TokenHash)
                .IsUnique();

            modelBuilder.Entity<ErrorLog>()
                .HasIndex(e => e.CreatedAtUtc);

            modelBuilder.Entity<ErrorLog>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<ErrorLog>()
                .HasIndex(e => e.UserId);

            modelBuilder.Entity<AuthAuditLog>()
                .HasIndex(e => e.CreatedAtUtc);

            modelBuilder.Entity<AuthAuditLog>()
                .HasIndex(e => e.Action);

            modelBuilder.Entity<AuthAuditLog>()
                .HasIndex(e => e.Outcome);

            modelBuilder.Entity<AuthAuditLog>()
                .HasIndex(e => e.UserId);

            modelBuilder.Entity<AuthAuditLog>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<Account>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value && !e.IsDeleted);

            modelBuilder.Entity<Category>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value);

            modelBuilder.Entity<Transaction>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value);

            modelBuilder.Entity<RecurringTransaction>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value);

            modelBuilder.Entity<UserReportPreference>()
                .HasIndex(e => e.TenantId)
                .IsUnique();

            modelBuilder.Entity<UserReportPreference>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value);

            modelBuilder.Entity<AiReportCache>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<AiReportCache>()
                .HasIndex(e => new { e.TenantId, e.Year, e.Month })
                .IsUnique();

            modelBuilder.Entity<AiReportCache>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value);

            modelBuilder.Entity<AppNotification>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<AppNotification>()
                .HasIndex(e => new { e.TenantId, e.IsRead });

            modelBuilder.Entity<AppNotification>()
                .HasIndex(e => e.DuplicateKey)
                .HasFilter("\"DuplicateKey\" IS NOT NULL");

            modelBuilder.Entity<AppNotification>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value);

            modelBuilder.Entity<AppNotification>()
                .Property(e => e.Type)
                .HasConversion<string>();

            modelBuilder.Entity<RecurringTransaction>()
                .Property(e => e.Frequency)
                .HasConversion<string>();

            modelBuilder.Entity<Budget>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value);

            modelBuilder.Entity<Budget>()
                .Property(e => e.PaycheckPeriod)
                .HasConversion<int>();

            modelBuilder.Entity<TransactionAttachment>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value);

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
            .HasIndex(b => new { b.TenantId, b.CategoryId, b.Year, b.Month })
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

            modelBuilder.Entity<CardBenefit>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<CardBenefit>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value && !e.IsDeleted);

            modelBuilder.Entity<CardBenefit>()
                .HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CardBenefit>()
                .HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Merchant
            modelBuilder.Entity<Merchant>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<Merchant>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value && !e.IsDeleted);

            // Transaction → Merchant FK
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Merchant)
                .WithMany()
                .HasForeignKey(t => t.MerchantId)
                .OnDelete(DeleteBehavior.SetNull);

            // TransactionRule
            modelBuilder.Entity<TransactionRule>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<TransactionRule>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value && !e.IsDeleted);

            modelBuilder.Entity<TransactionRule>()
                .HasMany(r => r.Conditions)
                .WithOne(c => c.Rule)
                .HasForeignKey(c => c.RuleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransactionRule>()
                .HasMany(r => r.Actions)
                .WithOne(a => a.Rule)
                .HasForeignKey(a => a.RuleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransactionRuleAction>()
                .HasOne(a => a.Merchant)
                .WithMany()
                .HasForeignKey(a => a.MerchantId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TransactionRuleAction>()
                .HasOne(a => a.Category)
                .WithMany()
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // SavingsGoal
            modelBuilder.Entity<SavingsGoal>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<SavingsGoal>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value && !e.IsDeleted);

            modelBuilder.Entity<SavingsGoal>()
                .Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            modelBuilder.Entity<SavingsGoal>()
                .Property(e => e.TargetAmount)
                .HasColumnType("numeric(12,2)");

            // SavingsContribution
            modelBuilder.Entity<SavingsContribution>()
                .HasOne(c => c.Goal)
                .WithMany(g => g.Contributions)
                .HasForeignKey(c => c.GoalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavingsContribution>()
                .Property(c => c.Amount)
                .HasColumnType("numeric(12,2)");

            modelBuilder.Entity<SavingsContribution>()
                .HasOne(c => c.LinkedTransaction)
                .WithMany()
                .HasForeignKey(c => c.LinkedTransactionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Loan
            modelBuilder.Entity<Loan>()
                .HasIndex(e => e.TenantId);

            modelBuilder.Entity<Loan>()
                .HasQueryFilter(e => CurrentTenantId.HasValue && e.TenantId == CurrentTenantId.Value && !e.IsDeleted);

            modelBuilder.Entity<Loan>()
                .Property(e => e.ContactName)
                .IsRequired()
                .HasMaxLength(200);

            modelBuilder.Entity<Loan>()
                .Property(e => e.PrincipalAmount)
                .HasColumnType("numeric(12,2)");

            modelBuilder.Entity<Loan>()
                .Property(e => e.InterestRate)
                .HasColumnType("numeric(8,4)");

            modelBuilder.Entity<Loan>()
                .Property(e => e.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Loan>()
                .Property(e => e.PaymentFrequency)
                .HasConversion<string>();

            // LoanPayment
            modelBuilder.Entity<LoanPayment>()
                .HasOne(p => p.Loan)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoanPayment>()
                .HasOne(p => p.Transaction)
                .WithMany()
                .HasForeignKey(p => p.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LoanPayment>()
                .Property(p => p.Amount)
                .HasColumnType("numeric(12,2)");

            // LoanInstallment
            modelBuilder.Entity<LoanInstallment>()
                .HasOne(i => i.Loan)
                .WithMany(l => l.Installments)
                .HasForeignKey(i => i.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoanInstallment>()
                .Property(i => i.ExpectedAmount)
                .HasColumnType("numeric(12,2)");

            modelBuilder.Entity<LoanInstallment>()
                .HasIndex(i => new { i.LoanId, i.InstallmentNumber })
                .IsUnique();
        }

        public override int SaveChanges()
        {
            ApplyTenantEnforcement();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyTenantEnforcement();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenantEnforcement();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyTenantEnforcement();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ApplyTenantEnforcement()
        {
            var tenantEntries = ChangeTracker.Entries<ITenantOwned>()
                .Where(entry =>
                    entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified ||
                    entry.State == EntityState.Deleted)
                .ToList();

            if (!CurrentTenantId.HasValue)
            {
                foreach (var entry in tenantEntries)
                {
                    if (entry.State != EntityState.Added || entry.Entity.TenantId == Guid.Empty)
                    {
                        throw new UnauthorizedAccessException("Tenant is required.");
                    }
                }

                return;
            }

            var tenantId = CurrentTenantId.Value;

            foreach (var entry in tenantEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.TenantId = tenantId;
                    continue;
                }

                if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    if (entry.Entity.TenantId != tenantId)
                    {
                        throw new UnauthorizedAccessException("Cross-tenant data access denied.");
                    }

                    // Prevent tenant hopping in updates.
                    entry.Property(nameof(ITenantOwned.TenantId)).IsModified = false;
                }
            }
        }
    }
}
