using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalContributionReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ContributionReminderAmount",
                table: "SavingsGoals",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContributionReminderDay",
                table: "SavingsGoals",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContributionReminderAmount",
                table: "SavingsGoals");

            migrationBuilder.DropColumn(
                name: "ContributionReminderDay",
                table: "SavingsGoals");
        }
    }
}
