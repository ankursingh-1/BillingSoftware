using Billing.API.Services.ProductImport;
using Billing.Application.DTOs.Reports;
using Billing.Application.Interfaces;
using ClosedXML.Excel;
using System.IO;

namespace Billing.API.Services;

public class ExcelExportService : IExcelExportService
{
    private static void Text(IXLCell cell, string? value)
    {
        ProductImportWorkbook.WriteText(cell, value);
    }

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
            Text(worksheet.Cell(row, 1), item.InvoiceNumber);
            Text(worksheet.Cell(row, 2), item.CustomerName);
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
            Text(worksheet.Cell(row, 1), item.PurchaseNumber);
            Text(worksheet.Cell(row, 2), item.SupplierName);
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
            Text(worksheet.Cell(row, 1), item.ProductName);
            Text(worksheet.Cell(row, 2), item.SKU);
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
        worksheet.Range(1, 1, 1, 6).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(3, 1).Value = "Invoice";
        worksheet.Cell(3, 2).Value = "Sale Date";
        worksheet.Cell(3, 3).Value = "Net sales (ex-tax)";
        worksheet.Cell(3, 4).Value = "Cost of goods sold";
        worksheet.Cell(3, 5).Value = "Profit";
        worksheet.Cell(3, 6).Value = "Cost data";
        worksheet.Range(3, 1, 3, 6).Style.Font.Bold = true;
        int row = 4;
        foreach (var item in report.Items)
        {
            Text(worksheet.Cell(row, 1), item.InvoiceNumber);
            worksheet.Cell(row, 2).Value = item.SaleDate;
            worksheet.Cell(row, 2).Style.DateFormat.Format = "dd-MM-yyyy";
            worksheet.Cell(row, 3).Value = item.SaleAmount;
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            if (item.CostOfGoodsSold.HasValue)
            {
                worksheet.Cell(row, 4).Value = item.CostOfGoodsSold.Value;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            }
            else
            {
                worksheet.Cell(row, 4).Value = "Cost unavailable";
            }

            if (item.Profit.HasValue)
            {
                worksheet.Cell(row, 5).Value = item.Profit.Value;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            }
            else
            {
                worksheet.Cell(row, 5).Value = "Cost unavailable";
            }

            Text(worksheet.Cell(row, 6), item.CostStatus);
            row++;
        }
        worksheet.Cell(row + 1, 1).Value = $"Net sales {report.TotalSales:0.00}; complete invoices {report.CompleteInvoices}; incomplete {report.IncompleteInvoices}; coverage {report.CoveragePercent:0.##}%";
        worksheet.Cell(row + 2, 4).Value = report.CostComplete ? "Gross profit" : "Known gross profit";
        worksheet.Cell(row + 2, 4).Style.Font.Bold = true;
        if (report.GrossProfit.HasValue)
        {
            worksheet.Cell(row + 2, 5).Value = report.GrossProfit.Value;
            worksheet.Cell(row + 2, 5).Style.NumberFormat.Format = "#,##0.00";
        }
        else
        {
            worksheet.Cell(row + 2, 5).Value = "Unavailable";
        }
        if (!string.IsNullOrEmpty(report.Limitation))
            Text(worksheet.Cell(row + 4, 1), report.Limitation);
        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportExpenseReport(ExpenseReportResponse report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Expenses");
        sheet.Cell(1, 1).Value = "Expense Report";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(3, 1).Value = "Date";
        sheet.Cell(3, 2).Value = "Title";
        sheet.Cell(3, 3).Value = "Category";
        sheet.Cell(3, 4).Value = "Amount";
        sheet.Range(3, 1, 3, 4).Style.Font.Bold = true;
        var row = 4;
        foreach (var item in report.Items)
        {
            sheet.Cell(row, 1).Value = item.ExpenseDate;
            sheet.Cell(row, 1).Style.DateFormat.Format = "dd-MM-yyyy";
            Text(sheet.Cell(row, 2), item.Title);
            Text(sheet.Cell(row, 3), item.Category);
            sheet.Cell(row, 4).Value = item.Amount;
            sheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
        sheet.Cell(row + 1, 3).Value = "Grand Total";
        sheet.Cell(row + 1, 3).Style.Font.Bold = true;
        sheet.Cell(row + 1, 4).Value = report.GrandTotal;
        sheet.Cell(row + 1, 4).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportCustomerReport(CustomerReportResponse report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Customers");
        sheet.Cell(1, 1).Value = "Customer Report";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(3, 1).Value = "Customer";
        sheet.Cell(3, 2).Value = "Mobile";
        sheet.Cell(3, 3).Value = "GSTIN";
        sheet.Cell(3, 4).Value = "Invoices";
        sheet.Cell(3, 5).Value = "Billed";
        sheet.Cell(3, 6).Value = "Outstanding";
        sheet.Range(3, 1, 3, 6).Style.Font.Bold = true;
        var row = 4;
        foreach (var item in report.Items)
        {
            Text(sheet.Cell(row, 1), item.Name);
            Text(sheet.Cell(row, 2), item.Mobile);
            Text(sheet.Cell(row, 3), item.Gstin);
            sheet.Cell(row, 4).Value = item.InvoiceCount;
            sheet.Cell(row, 5).Value = item.Billed;
            sheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(row, 6).Value = item.Outstanding;
            sheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
        sheet.Cell(row + 1, 5).Value = report.TotalBilled;
        sheet.Cell(row + 1, 6).Value = report.TotalOutstanding;
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportSupplierReport(SupplierReportResponse report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Suppliers");
        sheet.Cell(1, 1).Value = "Supplier Report";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        Text(sheet.Cell(2, 1), report.Limitation);
        sheet.Cell(3, 1).Value = "Supplier";
        sheet.Cell(3, 2).Value = "Company";
        sheet.Cell(3, 3).Value = "Mobile";
        sheet.Cell(3, 4).Value = "GSTIN";
        sheet.Cell(3, 5).Value = "Purchases";
        sheet.Cell(3, 6).Value = "Purchased";
        sheet.Range(3, 1, 3, 6).Style.Font.Bold = true;
        var row = 4;
        foreach (var item in report.Items)
        {
            Text(sheet.Cell(row, 1), item.Name);
            Text(sheet.Cell(row, 2), item.CompanyName);
            Text(sheet.Cell(row, 3), item.Mobile);
            Text(sheet.Cell(row, 4), item.Gstin);
            sheet.Cell(row, 5).Value = item.PurchaseCount;
            sheet.Cell(row, 6).Value = item.Purchased;
            sheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
        sheet.Cell(row + 1, 6).Value = report.TotalPurchased;
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportPaymentReport(PaymentReportResponse report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Collections");
        sheet.Cell(1, 1).Value = "Payment / collection report";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(3, 1).Value = "Receipt";
        sheet.Cell(3, 2).Value = "Invoice";
        sheet.Cell(3, 3).Value = "Customer";
        sheet.Cell(3, 4).Value = "Date";
        sheet.Cell(3, 5).Value = "Method";
        sheet.Cell(3, 6).Value = "Amount";
        sheet.Cell(3, 7).Value = "Reference";
        sheet.Range(3, 1, 3, 7).Style.Font.Bold = true;
        var row = 4;
        foreach (var item in report.Items)
        {
            Text(sheet.Cell(row, 1), item.ReceiptNumber);
            Text(sheet.Cell(row, 2), item.InvoiceNumber);
            Text(sheet.Cell(row, 3), item.CustomerName);
            sheet.Cell(row, 4).Value = item.PaymentDate;
            sheet.Cell(row, 4).Style.DateFormat.Format = "dd-MM-yyyy";
            Text(sheet.Cell(row, 5), item.Method);
            sheet.Cell(row, 6).Value = item.Amount;
            sheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            Text(sheet.Cell(row, 7), item.Reference);
            row++;
        }
        sheet.Cell(row + 1, 5).Value = "Total collected";
        sheet.Cell(row + 1, 6).Value = report.TotalCollected;
        sheet.Cell(row + 1, 6).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}