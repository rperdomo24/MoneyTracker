using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUncategorizedSystemCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill a per-tenant "Uncategorized" system category for tenants created
            // before this category existed (new tenants get it via TenantBootstrapService).
            migrationBuilder.Sql(@"
                INSERT INTO ""Categories"" (""TenantId"", ""Name"", ""Type"", ""Icon"", ""Color"", ""IsSystem"", ""SystemCategoryCode"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
                SELECT DISTINCT c.""TenantId"", 'Uncategorized', 2, 'Help', '#9E9E9E', true, 'UNCATEGORIZED', now(), now(), false
                FROM ""Categories"" c
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Categories"" c2
                    WHERE c2.""TenantId"" = c.""TenantId"" AND c2.""SystemCategoryCode"" = 'UNCATEGORIZED'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Categories""
                WHERE ""SystemCategoryCode"" = 'UNCATEGORIZED'
                  AND NOT EXISTS (SELECT 1 FROM ""Transaction"" t WHERE t.""CategoryId"" = ""Categories"".""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""Budgets"" b WHERE b.""CategoryId"" = ""Categories"".""Id"");
            ");
        }
    }
}
