using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Billing.Application.DTOs.Reports;

namespace Billing.Application.Interfaces;

public interface IPdfExportService
{
    byte[] ExportSalesReport(SalesReportResponse report);
    byte[] ExportPurchaseReport(PurchaseReportResponse report);
    byte[] ExportStockReport(StockReportResponse report);
    byte[] ExportProfitReport(ProfitReportResponse report);
}