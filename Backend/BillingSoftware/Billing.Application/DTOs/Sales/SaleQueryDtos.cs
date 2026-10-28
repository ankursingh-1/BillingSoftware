using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Sales
{
    public class SaleSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? CustomerId { get; set; }

        /// <summary>NotRecorded, Unpaid, Partial, Paid or Settled.</summary>
        public string? PaymentStatus { get; set; }
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
        public decimal ReturnedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Outstanding { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class SaleDetailDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string? Remarks { get; set; }
        public SaleCustomerDto Customer { get; set; } = new();
        public InvoiceCompanyDto Company { get; set; } = new();

        // ---- Amounts ----
        public decimal Subtotal { get; set; }
        public decimal CgstTotal { get; set; }
        public decimal SgstTotal { get; set; }
        public decimal IgstTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal RoundOff { get; set; }
        public decimal TotalAmount { get; set; }
        public string AmountInWords { get; set; } = string.Empty;

        /// <summary>False for legacy invoices: their tax was never recorded (it is unknown, not zero).</summary>
        public bool TaxCalculated { get; set; }
        public bool IsInterState { get; set; }
        public string? PlaceOfSupplyStateCode { get; set; }
        public string? PlaceOfSupplyState { get; set; }

        // ---- Payments ----
        public bool PaymentTracked { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal PaidAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal ReturnedAmount { get; set; }
        public decimal Outstanding { get; set; }
        public decimal RefundDue { get; set; }

        public List<SaleDetailItemDto> Items { get; set; } = new();
        public List<SalePaymentDto> Payments { get; set; } = new();
        public List<SaleReturnSummaryDto> Returns { get; set; } = new();
    }

    public class SaleCustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? State { get; set; }
        public string? StateCode { get; set; }
        public string? Gstin { get; set; }
    }

    /// <summary>Company details printed on the invoice (current values from Settings).</summary>
    public class InvoiceCompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? StateCode { get; set; }
        public string? Pincode { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Gstin { get; set; }
        public string? Pan { get; set; }
        public bool HasLogo { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IfscCode { get; set; }
        public string? UpiId { get; set; }
        public string? Terms { get; set; }
        public string? Footer { get; set; }
        public string Currency { get; set; } = "INR";
    }

    public class SaleDetailItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? HsnCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        /// <summary>Taxable value of the line (before tax).</summary>
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

    public class SalePaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public string? RecordedBy { get; set; }
        public bool IsVoided { get; set; }
        public DateTime? VoidedOn { get; set; }
        public string? VoidReason { get; set; }
    }

    public class SaleReturnSummaryDto
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
