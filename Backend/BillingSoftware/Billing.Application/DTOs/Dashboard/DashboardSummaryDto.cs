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
        public decimal GrossProfit { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalSuppliers { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
    }
}