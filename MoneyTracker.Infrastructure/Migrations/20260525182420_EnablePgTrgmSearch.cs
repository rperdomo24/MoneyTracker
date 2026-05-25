using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnablePgTrgmSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS idx_transaction_name_trgm
                ON "Transaction" USING GIN (lower("Name") gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS idx_transaction_desc_trgm
                ON "Transaction" USING GIN (lower("Description") gin_trgm_ops)
                WHERE "Description" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS idx_account_name_trgm
                ON "Accounts" USING GIN (lower("Name") gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS idx_category_name_trgm
                ON "Categories" USING GIN (lower("Name") gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_transaction_name_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_transaction_desc_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_account_name_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_category_name_trgm;");
        }
    }
}
