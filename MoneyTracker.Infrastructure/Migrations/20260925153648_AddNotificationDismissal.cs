using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDismissal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DismissedAt",
                table: "AppNotifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDismissed",
                table: "AppNotifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_TenantId_IsDismissed_IsRead",
                table: "AppNotifications",
                columns: new[] { "TenantId", "IsDismissed", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppNotifications_TenantId_IsDismissed_IsRead",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "DismissedAt",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "IsDismissed",
                table: "AppNotifications");
        }
    }
}
