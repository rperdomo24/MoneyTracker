using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGmailEmailImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailImportItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GmailMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ThreadId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    From = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BodyText = table.Column<string>(type: "text", nullable: false),
                    BodyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParsedJson = table.Column<string>(type: "text", nullable: true),
                    Fingerprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    AiTrainingDataId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailImportItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailImportRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderPattern = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    BankLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubjectIncludeKeywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubjectExcludeKeywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefaultAccountId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailImportRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailImportRules_Accounts_DefaultAccountId",
                        column: x => x.DefaultAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GmailConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "text", nullable: false),
                    LastSyncAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutoSyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmailConnections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailImportItems_TenantId",
                table: "EmailImportItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailImportItems_TenantId_Fingerprint",
                table: "EmailImportItems",
                columns: new[] { "TenantId", "Fingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailImportItems_TenantId_GmailMessageId",
                table: "EmailImportItems",
                columns: new[] { "TenantId", "GmailMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailImportItems_TenantId_Status",
                table: "EmailImportItems",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailImportRules_DefaultAccountId",
                table: "EmailImportRules",
                column: "DefaultAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailImportRules_TenantId",
                table: "EmailImportRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GmailConnections_TenantId",
                table: "GmailConnections",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailImportItems");

            migrationBuilder.DropTable(
                name: "EmailImportRules");

            migrationBuilder.DropTable(
                name: "GmailConnections");
        }
    }
}
