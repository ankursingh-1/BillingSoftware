using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGstSnapshotsAndDocumentSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- Safety pre-checks: stop with a clear message instead of failing half-way ----
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM Sales GROUP BY InvoiceNumber HAVING COUNT(*) > 1)
    THROW 50001, 'Migration stopped: duplicate invoice numbers exist in Sales. Resolve them, then re-run.', 1;
IF EXISTS (SELECT 1 FROM Purchases GROUP BY PurchaseNumber HAVING COUNT(*) > 1)
    THROW 50002, 'Migration stopped: duplicate purchase numbers exist in Purchases. Resolve them, then re-run.', 1;
IF EXISTS (SELECT 1 FROM Products WHERE Stock < 0)
    THROW 50003, 'Migration stopped: products with negative stock exist. Correct them with a stock adjustment, then re-run.', 1;
");
            migrationBuilder.AddColumn<string>(
                name: "Gstin",
                table: "Suppliers",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                table: "Suppliers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstTotal",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientRequestId",
                table: "Sales",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyGstin",
                table: "Sales",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyStateCode",
                table: "Sales",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerGstin",
                table: "Sales",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IgstTotal",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsInterState",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfSupplyStateCode",
                table: "Sales",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RoundOff",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstTotal",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "TaxCalculated",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxTotal",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstAmount",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "SaleItems",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IgstAmount",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstAmount",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxName",
                table: "SaleItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "SaleItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmount",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstTotal",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientRequestId",
                table: "Purchases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IgstTotal",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsInterState",
                table: "Purchases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RoundOff",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstTotal",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SupplierGstin",
                table: "Purchases",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierInvoiceNumber",
                table: "Purchases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TaxCalculated",
                table: "Purchases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxTotal",
                table: "Purchases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstAmount",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "PurchaseItems",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IgstAmount",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstAmount",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxName",
                table: "PurchaseItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "PurchaseItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmount",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "Products",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gstin",
                table: "Customers",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                table: "Customers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                table: "Companies",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentSequences",
                columns: table => new
                {
                    SeriesKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSequences", x => x.SeriesKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ClientRequestId",
                table: "Sales",
                column: "ClientRequestId",
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_InvoiceNumber",
                table: "Sales",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ClientRequestId",
                table: "Purchases",
                column: "ClientRequestId",
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PurchaseDate",
                table: "Purchases",
                column: "PurchaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PurchaseNumber",
                table: "Purchases",
                column: "PurchaseNumber",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Stock_NonNegative",
                table: "Products",
                sql: "[Stock] >= 0");

            // ---- Legacy data (no invented figures) ----
            // Sales/purchases created before GST snapshots existed charged no tax,
            // so what they charged equals their pre-tax value. They stay flagged
            // TaxCalculated = 0 ("tax not recorded"), never "zero tax".
            migrationBuilder.Sql("UPDATE Sales SET Subtotal = TotalAmount;");
            migrationBuilder.Sql("UPDATE Purchases SET Subtotal = TotalAmount;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentSequences");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ClientRequestId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_InvoiceNumber",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_ClientRequestId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_PurchaseDate",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_PurchaseNumber",
                table: "Purchases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Stock_NonNegative",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Gstin",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CgstTotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CompanyGstin",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CompanyStateCode",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CustomerGstin",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IgstTotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsInterState",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PlaceOfSupplyStateCode",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RoundOff",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SgstTotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TaxCalculated",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TaxTotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CgstAmount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "IgstAmount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "SgstAmount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "TaxName",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "TaxableAmount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "CgstTotal",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "IgstTotal",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "IsInterState",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "RoundOff",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SgstTotal",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SupplierGstin",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SupplierInvoiceNumber",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TaxCalculated",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TaxTotal",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "CgstAmount",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "IgstAmount",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "SgstAmount",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "TaxName",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "TaxableAmount",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Gstin",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "Companies");
        }
    }
}
