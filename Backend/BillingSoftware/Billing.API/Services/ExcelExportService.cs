using Billing.Application.DTOs.Reports;
using Billing.Application.Interfaces;
using ClosedXML.Excel;
using System.IO;

namespace Billing.API.Services;

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportSalesReport(SalesReportResponse report)
    {
        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Sales Report");
        worksheet.Cell(1, 1).Value = "Sales Report";
        worksheet.Range(1, 1, 1, 5).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(3, 1).Value = "Invoice No";
        worksheet.Cell(3, 2).Value = "Customer";
        worksheet.Cell(3, 3).Value = "Sale Date";
        worksheet.Cell(3, 4).Value = "Amount";
        worksheet.Range(3, 1, 3, 4).Style.Font.Bold = true;
        int row = 4;
        foreach (var item in report.Items)
        {
            worksheet.Cell(row, 1).Value = item.InvoiceNumber;
            worksheet.Cell(row, 2).Value = item.CustomerName;
            worksheet.Cell(row, 3).Value = item.SaleDate;
            worksheet.Cell(row, 3).Style.DateFormat.Format = "dd-MM-yyyy";
            worksheet.Cell(row, 4).Value = item.TotalAmount;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
        worksheet.Cell(row + 1, 3).Value = "Grand Total";
        worksheet.Cell(row + 1, 3).Style.Font.Bold = true;
        worksheet.Cell(row + 1, 4).Value = report.GrandTotal;
        worksheet.Cell(row + 1, 4).Style.Font.Bold = true;
        worksheet.Cell(row + 1, 4).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    public byte[] ExportPurchaseReport(PurchaseReportResponse report)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Purchase Report");
        worksheet.Cell(1, 1).Value = "Purchase Report";
        worksheet.Range(1, 1, 1, 5).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(3, 1).Value = "Invoice No";
        worksheet.Cell(3, 2).Value = "Supplier";
        worksheet.Cell(3, 3).Value = "Purchase Date";
        worksheet.Cell(3, 4).Value = "Amount";
        worksheet.Range(3, 1, 3, 4).Style.Font.Bold = true;
        int row = 4;
        foreach (var item in report.Items)
        {
            worksheet.Cell(row, 1).Value = item.PurchaseNumber;
            worksheet.Cell(row, 2).Value = item.SupplierName;
            worksheet.Cell(row, 3).Value = item.PurchaseDate;
            worksheet.Cell(row, 3).Style.DateFormat.Format = "dd-MM-yyyy";
            worksheet.Cell(row, 4).Value = item.TotalAmount;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
        worksheet.Cell(row + 1, 3).Value = "Grand Total";
        worksheet.Cell(row + 1, 3).Style.Font.Bold = true;
        worksheet.Cell(row + 1, 4).Value = report.GrandTotal;
        worksheet.Cell(row + 1, 4).Style.Font.Bold = true;
        worksheet.Cell(row + 1, 4).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    public byte[] ExportStockReport(StockReportResponse report)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Stock Report");
        worksheet.Cell(1, 1).Value = "Stock Report";
        worksheet.Range(1, 1, 1, 5).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(3, 1).Value = "Product";
        worksheet.Cell(3, 2).Value = "SKU";
        worksheet.Cell(3, 3).Value = "Stock";
        worksheet.Cell(3, 4).Value = "Purchase Price";
        worksheet.Cell(3, 5).Value = "Selling Price";
        worksheet.Range(3, 1, 3, 5).Style.Font.Bold = true;
        int row = 4;
        foreach (var item in report.Items)
        {
            worksheet.Cell(row, 1).Value = item.ProductName;
            worksheet.Cell(row, 2).Value = item.SKU;
            worksheet.Cell(row, 3).Value = item.CurrentStock;
            worksheet.Cell(row, 4).Value = item.PurchasePrice;
            worksheet.Cell(row, 5).Value = item.SellingPrice;
            row++;
        }
        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    public byte[] ExportProfitReport(ProfitReportResponse report)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Profit Report");
        worksheet.Cell(1, 1).Value = "Profit Report";
        worksheet.Range(1, 1, 1, 5).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(3, 1).Value = "Invoice";
        worksheet.Cell(3, 2).Value = "Sale Date";
        worksheet.Cell(3, 3).Value = "Sale Amount";
        worksheet.Cell(3, 4).Value = "Purchase Cost";
        worksheet.Cell(3, 5).Value = "Profit";
        worksheet.Range(3, 1, 3, 5).Style.Font.Bold = true;
        int row = 4;
        foreach (var item in report.Items)
        {
            worksheet.Cell(row, 1).Value = item.InvoiceNumber;
            worksheet.Cell(row, 2).Value = item.SaleDate;
            worksheet.Cell(row, 2).Style.DateFormat.Format = "dd-MM-yyyy";
            worksheet.Cell(row, 3).Value = item.SaleAmount;
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 4).Value = item.PurchaseCost;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 5).Value = item.Profit;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
        worksheet.Cell(row + 1, 4).Value = "Gross Profit";
        worksheet.Cell(row + 1, 4).Style.Font.Bold = true;
        worksheet.Cell(row + 1, 5).Value = report.GrossProfit;
        worksheet.Cell(row + 1, 5).Style.Font.Bold = true;
        worksheet.Cell(row + 1, 5).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}