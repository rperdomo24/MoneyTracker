using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitUncategorizedByIncomeExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace the single Expense-only "Uncategorized" category with one per
            // Income/Expense, so income transactions don't get misclassified as expenses
            // when they fall back to Uncategorized (TransactionType is derived from Category.Type).
            migrationBuilder.Sql(@"
                DELETE FROM ""Categories"" WHERE ""SystemCategoryCode"" = 'UNCATEGORIZED';

                INSERT INTO ""Categories"" (""TenantId"", ""Name"", ""Type"", ""Icon"", ""Color"", ""IsSystem"", ""SystemCategoryCode"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
                SELECT DISTINCT c.""TenantId"", 'Uncategorized', 1, 'Help', '#9E9E9E', true, 'UNCATEGORIZED_INCOME', now(), now(), false
                FROM ""Categories"" c
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Categories"" c2
                    WHERE c2.""TenantId"" = c.""TenantId"" AND c2.""SystemCategoryCode"" = 'UNCATEGORIZED_INCOME'
                );

                INSERT INTO ""Categories"" (""TenantId"", ""Name"", ""Type"", ""Icon"", ""Color"", ""IsSystem"", ""SystemCategoryCode"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
                SELECT DISTINCT c.""TenantId"", 'Uncategorized', 2, 'Help', '#9E9E9E', true, 'UNCATEGORIZED_EXPENSE', now(), now(), false
                FROM ""Categories"" c
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Categories"" c2
                    WHERE c2.""TenantId"" = c.""TenantId"" AND c2.""SystemCategoryCode"" = 'UNCATEGORIZED_EXPENSE'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Categories""
                WHERE ""SystemCategoryCode"" IN ('UNCATEGORIZED_INCOME', 'UNCATEGORIZED_EXPENSE')
                  AND NOT EXISTS (SELECT 1 FROM ""Transaction"" t WHERE t.""CategoryId"" = ""Categories"".""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""Budgets"" b WHERE b.""CategoryId"" = ""Categories"".""Id"");

                INSERT INTO ""Categories"" (""TenantId"", ""Name"", ""Type"", ""Icon"", ""Color"", ""IsSystem"", ""SystemCategoryCode"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
                SELECT DISTINCT c.""TenantId"", 'Uncategorized', 2, 'Help', '#9E9E9E', true, 'UNCATEGORIZED', now(), now(), false
                FROM ""Categories"" c
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Categories"" c2
                    WHERE c2.""TenantId"" = c.""TenantId"" AND c2.""SystemCategoryCode"" = 'UNCATEGORIZED'
                );
            ");
        }
    }
}
