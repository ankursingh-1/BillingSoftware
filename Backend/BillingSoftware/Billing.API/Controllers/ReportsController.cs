using Billing.API.Services;
using Billing.Application.DTOs.Common;
using Billing.Application.DTOs.Expense;
using Billing.Application.DTOs.Reports;
using Billing.Application.Interfaces;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string Pdf = "application/pdf";

    private readonly ReportService _reportService;
    private readonly IExcelExportService _excel;
    private readonly IPdfExportService _pdf;

    public ReportsController(
        ReportService reportService,
        IExcelExportService excel,
        IPdfExportService pdf)
    {
        _reportService = reportService;
        _excel = excel;
        _pdf = pdf;
    }

    [HttpPost("sales")]
    public async Task<IActionResult> GetSalesReport([FromBody] SalesReportRequest request) =>
        Ok(await _reportService.GetSalesReportAsync(request));

    [HttpPost("sales/excel")]
    public async Task<IActionResult> SalesExcel([FromBody] SalesReportRequest request)
    {
        PrepareExport(request);
        var result = await _reportService.GetSalesReportAsync(request);
        return File(_excel.ExportSalesReport(result), Xlsx, "sales-report.xlsx");
    }

    [HttpPost("sales/pdf")]
    public async Task<IActionResult> SalesPdf([FromBody] SalesReportRequest request)
    {
        PrepareExport(request);
        var result = await _reportService.GetSalesReportAsync(request);
        return File(_pdf.ExportSalesReport(result), Pdf, "sales-report.pdf");
    }

    [HttpPost("purchases")]
    public async Task<IActionResult> GetPurchaseReport([FromBody] PurchaseReportRequest request) =>
        Ok(await _reportService.GetPurchaseReportAsync(request));

    [HttpPost("purchases/excel")]
    public async Task<IActionResult> PurchasesExcel([FromBody] PurchaseReportRequest request)
    {
        PrepareExport(request);
        var result = await _reportService.GetPurchaseReportAsync(request);
        return File(_excel.ExportPurchaseReport(result), Xlsx, "purchase-report.xlsx");
    }

    [HttpPost("stock")]
    public async Task<IActionResult> GetStockReport([FromBody] StockReportRequest request) =>
        Ok(await _reportService.GetStockReportAsync(request));

    [HttpPost("stock/excel")]
    public async Task<IActionResult> StockExcel([FromBody] StockReportRequest request)
    {
        PrepareExport(request);
        var result = await _reportService.GetStockReportAsync(request);
        return File(_excel.ExportStockReport(result), Xlsx, "stock-report.xlsx");
    }

    [HttpPost("profit")]
    public async Task<IActionResult> GetProfitReport([FromBody] ProfitReportRequest request) =>
        Ok(await _reportService.GetProfitReportAsync(request));

    [HttpPost("profit/excel")]
    public async Task<IActionResult> ProfitExcel([FromBody] ProfitReportRequest request)
    {
        var result = await _reportService.GetProfitReportAsync(request);
        return File(_excel.ExportProfitReport(result), Xlsx, "profit-report.xlsx");
    }

    [HttpPost("profit/pdf")]
    public async Task<IActionResult> ProfitPdf([FromBody] ProfitReportRequest request)
    {
        var result = await _reportService.GetProfitReportAsync(request);
        return File(_pdf.ExportProfitReport(result), Pdf, "profit-report.pdf");
    }

    [HttpPost("expenses")]
    public async Task<IActionResult> Expenses([FromBody] ExpenseSearchRequest request) =>
        Ok(await _reportService.GetExpenseReportAsync(request));

    [HttpPost("expenses/excel")]
    public async Task<IActionResult> ExpensesExcel([FromBody] ExpenseSearchRequest request)
    {
        PrepareExport(request);
        var result = await _reportService.GetExpenseReportAsync(request);
        return File(_excel.ExportExpenseReport(result), Xlsx, "expense-report.xlsx");
    }

    [HttpPost("customers")]
    public async Task<IActionResult> Customers([FromBody] PartyReportRequest request) =>
        Ok(await _reportService.GetCustomerReportAsync(request));

    [HttpPost("customers/excel")]
    public async Task<IActionResult> CustomersExcel([FromBody] PartyReportRequest request)
    {
        PrepareExport(request);
        var result = await _reportService.GetCustomerReportAsync(request);
        return File(_excel.ExportCustomerReport(result), Xlsx, "customer-report.xlsx");
    }

    [HttpPost("suppliers")]
    public async Task<IActionResult> Suppliers([FromBody] PartyReportRequest request) =>
        Ok(await _reportService.GetSupplierReportAsync(request));

    [HttpPost("suppliers/excel")]
    public async Task<IActionResult> SuppliersExcel([FromBody] PartyReportRequest request)
    {
        PrepareExport(request);
        var result = await _reportService.GetSupplierReportAsync(request);
        return File(_excel.ExportSupplierReport(result), Xlsx, "supplier-report.xlsx");
    }

    [HttpPost("payments")]
    public async Task<IActionResult> Payments([FromBody] PaymentReportRequest request) =>
        Ok(await _reportService.GetPaymentReportAsync(request));

    [HttpPost("payments/excel")]
    public async Task<IActionResult> PaymentsExcel([FromBody] PaymentReportRequest request)
    {
        PrepareExport(request);
        var result = await _reportService.GetPaymentReportAsync(request);
        return File(_excel.ExportPaymentReport(result), Xlsx, "payment-report.xlsx");
    }

    /// <summary>
    /// Exports the full filtered set. The on-screen page number must not skip rows.
    /// </summary>
    private static void PrepareExport(PaginationRequest request)
    {
        request.PageNumber = 1;
        request.PageSize = 10_000;
    }
}
