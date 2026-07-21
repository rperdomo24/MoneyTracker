using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCallLogAndTextImportCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiCallLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Service = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    CachedTokens = table.Column<int>(type: "integer", nullable: false),
                    WasCacheHit = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCallLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiTextImportCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputHash = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTextImportCaches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiCallLogs_CalledAtUtc",
                table: "AiCallLogs",
                column: "CalledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiCallLogs_TenantId",
                table: "AiCallLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTextImportCaches_TenantId",
                table: "AiTextImportCaches",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTextImportCaches_TenantId_InputHash",
                table: "AiTextImportCaches",
                columns: new[] { "TenantId", "InputHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiCallLogs");

            migrationBuilder.DropTable(
                name: "AiTextImportCaches");
        }
    }
}
