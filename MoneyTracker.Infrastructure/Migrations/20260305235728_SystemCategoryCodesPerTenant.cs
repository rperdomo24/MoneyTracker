using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SystemCategoryCodesPerTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SystemCategoryCode",
                table: "Categories",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TenantId_SystemCategoryCode",
                table: "Categories",
                columns: new[] { "TenantId", "SystemCategoryCode" },
                unique: true,
                filter: "\"SystemCategoryCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_TenantId_SystemCategoryCode",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SystemCategoryCode",
                table: "Categories");
        }
    }
}
