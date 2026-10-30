using Billing.API.Services;
using Billing.Application.Accounting;
using Billing.Application.DTOs.Reports;
using ClosedXML.Excel;

namespace Billing.Tests;

public class GrossProfitExportTests
{
    [Fact]
    public void Excel_matches_the_calculator_for_complete_and_unavailable_rows()
    {
        var complete = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, 40m)
        ]);
        var missing = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 80m, null)
        ]);
        var summary = GrossProfitCalculator.Summarise([complete, missing], 1, 1);

        var report = new ProfitReportResponse
        {
            TotalSales = summary.NetRevenue,
            CostOfGoodsSold = summary.KnownCostOfGoodsSold,
            GrossProfit = summary.KnownGrossProfit,
            CompleteInvoices = summary.CompleteInvoices,
            IncompleteInvoices = summary.IncompleteInvoices,
            CoveragePercent = summary.CoveragePercent,
            LinesWithCost = summary.LinesWithCost,
            LinesMissingCost = summary.LinesMissingCost,
            CostComplete = summary.CostComplete,
            Limitation = summary.Limitation,
            Items =
            [
                new ProfitReportItemDto
                {
                    SaleId = 1,
                    InvoiceNumber = "INV-C",
                    SaleDate = new DateTime(2026, 10, 1),
                    SaleAmount = complete.NetRevenue,
                    CostOfGoodsSold = complete.CostOfGoodsSold,
                    Profit = complete.Profit,
                    CostComplete = complete.CostComplete,
                    CostStatus = complete.CostStatus
                },
                new ProfitReportItemDto
                {
                    SaleId = 2,
                    InvoiceNumber = "INV-U",
                    SaleDate = new DateTime(2026, 10, 2),
                    SaleAmount = missing.NetRevenue,
                    CostOfGoodsSold = missing.CostOfGoodsSold,
                    Profit = missing.Profit,
                    CostComplete = missing.CostComplete,
                    CostStatus = missing.CostStatus
                }
            ]
        };

        var bytes = new ExcelExportService().ExportProfitReport(report);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);

        Assert.Equal(100m, sheet.Cell(4, 3).GetValue<decimal>());
        Assert.Equal(60m, sheet.Cell(4, 5).GetValue<decimal>());
        Assert.Equal(GrossProfitCalculator.CompleteStatus, sheet.Cell(4, 6).GetString());
        Assert.Equal("Cost unavailable", sheet.Cell(5, 4).GetString());
        Assert.Equal("Cost unavailable", sheet.Cell(5, 5).GetString());
        Assert.Equal(GrossProfitCalculator.UnavailableStatus, sheet.Cell(5, 6).GetString());
        Assert.Contains("incomplete 1", sheet.Cell(7, 1).GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Known gross profit", sheet.Cell(8, 4).GetString());
        Assert.Equal(60m, sheet.Cell(8, 5).GetValue<decimal>());
        Assert.Contains("not calculated as sales minus purchases", sheet.Cell(10, 1).GetString());
    }

    [Fact]
    public void Excel_says_unavailable_when_no_complete_invoice_exists()
    {
        var missing = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 80m, null)
        ]);
        var summary = GrossProfitCalculator.Summarise([missing], 0, 1);
        var report = new ProfitReportResponse
        {
            TotalSales = summary.NetRevenue,
            GrossProfit = summary.KnownGrossProfit,
            CostComplete = summary.CostComplete,
            Limitation = summary.Limitation,
            Items =
            [
                new ProfitReportItemDto
                {
                    InvoiceNumber = "INV-U",
                    SaleDate = DateTime.Today,
                    SaleAmount = missing.NetRevenue,
                    CostStatus = missing.CostStatus
                }
            ]
        };

        var bytes = new ExcelExportService().ExportProfitReport(report);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);

        Assert.Null(summary.KnownGrossProfit);
        Assert.Equal("Unavailable", sheet.Cell(7, 5).GetString());
    }
}
