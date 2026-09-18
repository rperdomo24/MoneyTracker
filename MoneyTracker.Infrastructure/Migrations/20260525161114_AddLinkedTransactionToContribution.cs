using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedTransactionToContribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedTransactionId",
                table: "SavingsContributions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavingsContributions_LinkedTransactionId",
                table: "SavingsContributions",
                column: "LinkedTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SavingsContributions_Transaction_LinkedTransactionId",
                table: "SavingsContributions",
                column: "LinkedTransactionId",
                principalTable: "Transaction",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavingsContributions_Transaction_LinkedTransactionId",
                table: "SavingsContributions");

            migrationBuilder.DropIndex(
                name: "IX_SavingsContributions_LinkedTransactionId",
                table: "SavingsContributions");

            migrationBuilder.DropColumn(
                name: "LinkedTransactionId",
                table: "SavingsContributions");
        }
    }
}
