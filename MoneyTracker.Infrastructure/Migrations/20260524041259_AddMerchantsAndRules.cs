using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantsAndRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MerchantId",
                table: "Transaction",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ApplyToHistorical = table.Column<bool>(type: "boolean", nullable: false),
                    ApplyFromDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionRuleActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleId = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    MerchantId = table.Column<int>(type: "integer", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    StringValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionRuleActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionRuleActions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TransactionRuleActions_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TransactionRuleActions_TransactionRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "TransactionRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionRuleConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleId = table.Column<int>(type: "integer", nullable: false),
                    Field = table.Column<int>(type: "integer", nullable: false),
                    Operator = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionRuleConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionRuleConditions_TransactionRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "TransactionRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_MerchantId",
                table: "Transaction",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_TenantId",
                table: "Merchants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRuleActions_CategoryId",
                table: "TransactionRuleActions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRuleActions_MerchantId",
                table: "TransactionRuleActions",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRuleActions_RuleId",
                table: "TransactionRuleActions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRuleConditions_RuleId",
                table: "TransactionRuleConditions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRules_TenantId",
                table: "TransactionRules",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Merchants_MerchantId",
                table: "Transaction",
                column: "MerchantId",
                principalTable: "Merchants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Merchants_MerchantId",
                table: "Transaction");

            migrationBuilder.DropTable(
                name: "TransactionRuleActions");

            migrationBuilder.DropTable(
                name: "TransactionRuleConditions");

            migrationBuilder.DropTable(
                name: "Merchants");

            migrationBuilder.DropTable(
                name: "TransactionRules");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_MerchantId",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "Transaction");
        }
    }
}
