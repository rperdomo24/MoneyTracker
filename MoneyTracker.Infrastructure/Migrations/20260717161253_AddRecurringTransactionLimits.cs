using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTransactionLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "RecurringTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OccurrencesGenerated",
                table: "RecurringTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalOccurrences",
                table: "RecurringTransactions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "OccurrencesGenerated",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "TotalOccurrences",
                table: "RecurringTransactions");
        }
    }
}
