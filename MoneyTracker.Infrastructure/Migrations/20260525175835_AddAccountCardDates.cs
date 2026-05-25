using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountCardDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CutDay",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInNetWorth",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("UPDATE \"Accounts\" SET \"IncludeInNetWorth\" = TRUE");

            migrationBuilder.AddColumn<int>(
                name: "PaymentDay",
                table: "Accounts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CutDay",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IncludeInNetWorth",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PaymentDay",
                table: "Accounts");
        }
    }
}
