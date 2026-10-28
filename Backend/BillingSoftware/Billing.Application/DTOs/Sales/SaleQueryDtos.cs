using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Sales
{
    /// <summary>Query-string filters for GET /api/Sales.</summary>
    public class SaleSearchRequest : PaginationRequest
    {
        /// <summary>Matches invoice number or customer name.</summary>
        public string? Search { get; set; }

        /// <summary>Inclusive; compared by calendar date.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Inclusive; compared by calendar date.</summary>
        public DateTime? ToDate { get; set; }

        public int? CustomerId { get; set; }
    }

    public class SaleListItemDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }

    public class SaleDetailDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Remarks { get; set; }
        public SaleCustomerDto Customer { get; set; } = new();
        public List<SaleDetailItemDto> Items { get; set; } = new();
    }

    public class SaleCustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
    }

    public class SaleDetailItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}
