using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardBenefitsAndAccountBankName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Accounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CardBenefits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    MerchantPattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BenefitType = table.Column<int>(type: "integer", nullable: false),
                    BenefitRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    MaxMonthlyCap = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardBenefits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardBenefits_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardBenefits_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardBenefits_AccountId",
                table: "CardBenefits",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CardBenefits_CategoryId",
                table: "CardBenefits",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CardBenefits_TenantId",
                table: "CardBenefits",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardBenefits");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Accounts");
        }
    }
}
