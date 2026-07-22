using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTrainingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiTrainingData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServiceType = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    InputType = table.Column<string>(type: "text", nullable: false),
                    RawInput = table.Column<string>(type: "text", nullable: false),
                    OutputType = table.Column<string>(type: "text", nullable: false),
                    RawOutput = table.Column<string>(type: "text", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    UserFeedback = table.Column<string>(type: "text", nullable: true),
                    FeedbackAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTrainingData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiTrainingData_TenantId",
                table: "AiTrainingData",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTrainingData_TenantId_ServiceType_CalledAtUtc",
                table: "AiTrainingData",
                columns: new[] { "TenantId", "ServiceType", "CalledAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiTrainingData");
        }
    }
}
