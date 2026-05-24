using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardDisplayNameToAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardDisplayName",
                table: "Accounts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardDisplayName",
                table: "Accounts");
        }
    }
}
