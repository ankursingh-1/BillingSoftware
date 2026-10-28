namespace Billing.Application.DTOs.Purchases
{
    public class PurchaseListItemDto
    {
        public int Id { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public string? SupplierInvoiceNumber { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public decimal ReturnedAmount { get; set; }
    }

    public class PurchaseDetailDto
    {
        public int Id { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public string? SupplierInvoiceNumber { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }

        public PurchaseSupplierDto Supplier { get; set; } = new();

        public decimal Subtotal { get; set; }
        public decimal CgstTotal { get; set; }
        public decimal SgstTotal { get; set; }
        public decimal IgstTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal RoundOff { get; set; }
        public decimal TotalAmount { get; set; }

        /// <summary>False for purchases saved before tax was recorded (tax unknown, not zero).</summary>
        public bool TaxCalculated { get; set; }
        public bool IsInterState { get; set; }
        public decimal ReturnedAmount { get; set; }

        public List<PurchaseDetailItemDto> Items { get; set; } = new();
        public List<PurchaseReturnSummaryDto> Returns { get; set; } = new();
    }

    public class PurchaseSupplierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? State { get; set; }
        public string? StateCode { get; set; }
        public string? Gstin { get; set; }
    }

    public class PurchaseDetailItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? HsnCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        /// <summary>Taxable (pre-tax) amount.</summary>
        public decimal Total { get; set; }
        public string? TaxName { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? CgstAmount { get; set; }
        public decimal? SgstAmount { get; set; }
        public decimal? IgstAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? LineTotal { get; set; }
        public int ReturnedQuantity { get; set; }
        public int ReturnableQuantity { get; set; }
    }

    public class PurchaseReturnSummaryDto
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundReceived { get; set; }
    }
}