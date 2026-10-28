using Billing.Application.DTOs.Reports;

namespace Billing.Application.Interfaces;

public interface IExcelExportService
{
    byte[] ExportSalesReport(SalesReportResponse report);
    byte[] ExportPurchaseReport(PurchaseReportResponse report);
    byte[] ExportStockReport(StockReportResponse report);
    byte[] ExportProfitReport(ProfitReportResponse report);
    byte[] ExportExpenseReport(ExpenseReportResponse report);
    byte[] ExportCustomerReport(CustomerReportResponse report);
    byte[] ExportSupplierReport(SupplierReportResponse report);
    byte[] ExportPaymentReport(PaymentReportResponse report);
}