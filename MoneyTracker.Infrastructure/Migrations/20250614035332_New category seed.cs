using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Newcategoryseed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "Icon", "Name", "ParentId", "Type" },
                values: new object[,]
                {
                    { 4, "#4CAF50", "account_balance_wallet", "Initial Balance", null, 1 },
                    { 5, "#F44336", "account_balance_wallet", "Initial Balance", null, 2 },
                    { 6, "#4CAF50", "tune", "Balance Adjustment", null, 1 },
                    { 7, "#F44336", "tune", "Balance Adjustment", null, 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
