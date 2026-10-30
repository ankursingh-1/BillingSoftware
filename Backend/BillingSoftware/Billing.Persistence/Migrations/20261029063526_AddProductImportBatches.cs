using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImportBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    modifieson = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductImportReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ReceiptKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    modifieson = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImportReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImportReceipts_ProductImportBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductImportReceipts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImportBatches_FileHash",
                table: "ProductImportBatches",
                column: "FileHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImportReceipts_BatchId",
                table: "ProductImportReceipts",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImportReceipts_ProductId",
                table: "ProductImportReceipts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImportReceipts_ReceiptKey",
                table: "ProductImportReceipts",
                column: "ReceiptKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductImportReceipts");

            migrationBuilder.DropTable(
                name: "ProductImportBatches");
        }
    }
}
