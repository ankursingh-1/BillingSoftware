using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSnapshotReceiptsAndPrintSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentSnapshot",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "Payments",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultInvoiceFormat",
                table: "Companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "A4");

            migrationBuilder.AddColumn<bool>(
                name: "ShowBankDetails",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowFooter",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowLogo",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTerms",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ThermalPaperWidthMm",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 80);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments",
                column: "ReceiptNumber",
                unique: true,
                filter: "[ReceiptNumber] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Companies_ThermalWidth",
                table: "Companies",
                sql: "[ThermalPaperWidthMm] IN (58, 80)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Companies_ThermalWidth",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DocumentSnapshot",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DefaultInvoiceFormat",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ShowBankDetails",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ShowFooter",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ShowLogo",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ShowTerms",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ThermalPaperWidthMm",
                table: "Companies");
        }
    }
}
