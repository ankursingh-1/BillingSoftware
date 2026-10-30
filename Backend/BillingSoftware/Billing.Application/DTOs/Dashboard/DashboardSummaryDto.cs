namespace Billing.Application.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public decimal TotalSales { get; set; }
        public decimal TodaySales { get; set; }
        public decimal MonthlySales { get; set; }
        public decimal TotalPurchase { get; set; }
        public decimal TodayPurchase { get; set; }
        public decimal MonthlyPurchase { get; set; }
        /// <summary>
        /// Known gross profit this month (complete invoices only). Null when no
        /// complete invoice exists and some costs are missing — never coerced to 0.
        /// </summary>
        public decimal? GrossProfit { get; set; }
        public bool GrossProfitComplete { get; set; }
        public int GrossProfitIncompleteInvoices { get; set; }
        public string? GrossProfitLimitation { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalSuppliers { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }

        /// <summary>Outstanding on payment-tracked invoices (voided payments excluded).</summary>
        public decimal OutstandingCollections { get; set; }

        public int ExpiringWarranties { get; set; }

        /// <summary>
        /// False when this database does not have the Warranties table yet.
        /// Zero with true means there are genuinely no expiring rows.
        /// </summary>
        public bool ExpiringWarrantiesAvailable { get; set; } = true;
    }
}