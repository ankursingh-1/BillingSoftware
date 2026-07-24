using Billing.Application.DTOs.Reports;
using Billing.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Billing.API.Services;

public class PdfExportService : IPdfExportService
{
    public byte[] ExportSalesReport(SalesReportResponse report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("Sales Report")
                    .FontSize(22)
                    .Bold();

                page.Content()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Invoice").Bold();
                            header.Cell().Text("Customer").Bold();
                            header.Cell().Text("Date").Bold();
                            header.Cell().Text("Amount").Bold();
                        });

                        foreach (var item in report.Items)
                        {
                            table.Cell().Text(item.InvoiceNumber);
                            table.Cell().Text(item.CustomerName);
                            table.Cell().Text(item.SaleDate.ToString("dd-MM-yyyy"));
                            table.Cell().Text(item.TotalAmount.ToString("0.00"));
                        }
                    });

                page.Footer()
                    .AlignRight()
                    .Text($"Grand Total : {report.GrandTotal:0.00}");
            });
        }).GeneratePdf();
    }

    public byte[] ExportPurchaseReport(PurchaseReportResponse report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("Purchase Report")
                    .FontSize(22)
                    .Bold();

                page.Content()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Purchase No").Bold();
                            header.Cell().Text("Supplier").Bold();
                            header.Cell().Text("Date").Bold();
                            header.Cell().Text("Amount").Bold();
                        });

                        foreach (var item in report.Items)
                        {
                            table.Cell().Text(item.PurchaseNumber);
                            table.Cell().Text(item.SupplierName);
                            table.Cell().Text(item.PurchaseDate.ToString("dd-MM-yyyy"));
                            table.Cell().Text(item.TotalAmount.ToString("0.00"));
                        }
                    });

                page.Footer()
                    .AlignRight()
                    .Text($"Grand Total : {report.GrandTotal:0.00}");
            });
        }).GeneratePdf();
    }

    public byte[] ExportStockReport(StockReportResponse report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("Stock Report")
                    .FontSize(22)
                    .Bold();

                page.Content()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Product").Bold();
                            header.Cell().Text("SKU").Bold();
                            header.Cell().Text("Stock").Bold();
                            header.Cell().Text("Value").Bold();
                        });

                        foreach (var item in report.Items)
                        {
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.SKU);
                            table.Cell().Text(item.CurrentStock.ToString());
                            table.Cell().Text(item.StockValue.ToString("0.00"));
                        }
                    });

                page.Footer()
                    .AlignRight()
                    .Text($"Total Stock Value : {report.TotalStockValue:0.00}");
            });
        }).GeneratePdf();
    }

    public byte[] ExportProfitReport(ProfitReportResponse report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("Profit Report")
                    .FontSize(22)
                    .Bold();

                page.Content()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Invoice").Bold();
                            header.Cell().Text("Date").Bold();
                            header.Cell().Text("Sale").Bold();
                            header.Cell().Text("Cost").Bold();
                            header.Cell().Text("Profit").Bold();
                        });

                        foreach (var item in report.Items)
                        {
                            table.Cell().Text(item.InvoiceNumber);
                            table.Cell().Text(item.SaleDate.ToString("dd-MM-yyyy"));
                            table.Cell().Text(item.SaleAmount.ToString("0.00"));
                            table.Cell().Text(item.PurchaseCost.ToString("0.00"));
                            table.Cell().Text(item.Profit.ToString("0.00"));
                        }
                    });

                page.Footer()
                    .Column(column =>
                    {
                        column.Item().AlignRight().Text($"Total Sales : {report.TotalSales:0.00}");
                        column.Item().AlignRight().Text($"Purchase Cost : {report.TotalPurchaseCost:0.00}");
                        column.Item().AlignRight().Text($"Gross Profit : {report.GrossProfit:0.00}");
                        column.Item().AlignRight().Text($"Profit % : {report.ProfitPercentage:0.00}%");
                    });
            });
        }).GeneratePdf();
    }
}