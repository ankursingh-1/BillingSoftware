using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Inventory
{
    public class StockSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }

        /// <summary>all (default), in, low or out.</summary>
        public string? Status { get; set; }
    }

    public class StockItemDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? UnitName { get; set; }
        public int Stock { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }

        /// <summary>Stock x catalogue cost. Null when the product has no cost price set.</summary>
        public decimal? StockValue { get; set; }
    }

    public class StockSummaryDto
    {
        public int TotalProducts { get; set; }
        public int InStock { get; set; }
        public int LowStock { get; set; }
        public int OutOfStock { get; set; }
        public int LowStockThreshold { get; set; }
        public int TotalUnits { get; set; }

        /// <summary>Sum of stock x catalogue cost for products that have a cost.</summary>
        public decimal StockValueAtCost { get; set; }
        public int ProductsWithoutCost { get; set; }
    }
}