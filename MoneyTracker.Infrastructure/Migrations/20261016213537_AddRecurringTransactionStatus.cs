using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Expand step of expand/contract: adds "Status" and backfills it from "IsActive", but intentionally does NOT
    /// drop "IsActive" (EF scaffolds a DropColumn here; it was removed by hand to keep production data intact).
    /// The column is no longer mapped by the model, so it keeps a default to let new inserts succeed.
    /// Dropping it is a separate, later migration.
    /// </remarks>
    public partial class AddRecurringTransactionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "RecurringTransactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            // IsActive = true stays 'Active' (column default). Inactive rows are Paused...
            migrationBuilder.Sql(@"
                UPDATE ""RecurringTransactions""
                SET ""Status"" = 'Paused'
                WHERE ""IsActive"" = false;");

            // ...unless they already finished their installments / passed their end date, which means Completed.
            migrationBuilder.Sql(@"
                UPDATE ""RecurringTransactions""
                SET ""Status"" = 'Completed'
                WHERE ""IsActive"" = false
                  AND (
                        (""TotalOccurrences"" IS NOT NULL AND ""OccurrencesGenerated"" >= ""TotalOccurrences"")
                     OR (""EndDate"" IS NOT NULL
                         AND (""NextDate"" AT TIME ZONE 'UTC')::date > (""EndDate"" AT TIME ZONE 'UTC')::date)
                  );");

            // The model no longer maps IsActive; keep the old column insertable until it is dropped later.
            migrationBuilder.Sql(@"ALTER TABLE ""RecurringTransactions"" ALTER COLUMN ""IsActive"" SET DEFAULT true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // IsActive was kept, so restore it from Status before Status goes away.
            migrationBuilder.Sql(@"
                UPDATE ""RecurringTransactions""
                SET ""IsActive"" = (""Status"" = 'Active');");

            migrationBuilder.Sql(@"ALTER TABLE ""RecurringTransactions"" ALTER COLUMN ""IsActive"" DROP DEFAULT;");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "RecurringTransactions");
        }
    }
}
