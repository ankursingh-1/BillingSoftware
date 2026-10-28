using System.ComponentModel.DataAnnotations;
using Billing.Application.DTOs.Common;
using Billing.Domain.Enums;

namespace Billing.Application.DTOs.Returns
{
    // ---------------------------------------------------------------- Sales returns (credit notes)

    public class CreateSalesReturnRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select the invoice being returned against.")]
        public int SaleId { get; set; }

        [Required(ErrorMessage = "At least one item is required.")]
        [MinLength(1, ErrorMessage = "At least one item is required.")]
        [MaxLength(200, ErrorMessage = "A return cannot have more than 200 items.")]
        public List<ReturnSaleItemRequest> Items { get; set; } = new();

        [Required(ErrorMessage = "Enter a reason for the return.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "The reason must be 3 to 500 characters.")]
        public string? Reason { get; set; }

        /// <summary>Money handed back to the customer now. Leave 0 to settle it later.</summary>
        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Refund cannot be negative.")]
        public decimal RefundAmount { get; set; }

        public PaymentMethod? RefundMethod { get; set; }
    }

    public class ReturnSaleItemRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select an invoice line.")]
        public int SaleItemId { get; set; }

        [Range(1, 1_000_000, ErrorMessage = "Return quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    public class RecordRefundRequest
    {
        [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Refund amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Select a valid refund method.")]
        public PaymentMethod Method { get; set; }
    }

    public class SalesReturnSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SaleId { get; set; }
    }

    public class SalesReturnResponse
    {
        public int ReturnId { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal RefundAmount { get; set; }

        /// <summary>Still owed to the customer after this return and refund.</summary>
        public decimal RefundDue { get; set; }

        /// <summary>Still owed BY the customer on the invoice after this return.</summary>
        public decimal Outstanding { get; set; }
    }

    public class SalesReturnListItemDto
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class SalesReturnDetailDto
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public string? Reason { get; set; }
        public string? CreatedBy { get; set; }

        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerGstin { get; set; }
        public bool IsInterState { get; set; }

        public decimal Subtotal { get; set; }
        public decimal CgstTotal { get; set; }
        public decimal SgstTotal { get; set; }
        public decimal IgstTotal { get; set; }
        public decimal TaxTotal { get; set; }
        /// <summary>Non-zero only on the return that completes the whole invoice.</summary>
        public decimal RoundOff { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal RefundAmount { get; set; }
        public string? RefundMethod { get; set; }
        /// <summary>Still owed to the customer on the invoice (after all returns and refunds).</summary>
        public decimal RefundDue { get; set; }

        public List<ReturnItemDetailDto> Items { get; set; } = new();
    }

    // ---------------------------------------------------------------- Purchase returns (debit notes)

    public class CreatePurchaseReturnRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select the purchase being returned against.")]
        public int PurchaseId { get; set; }

        [Required(ErrorMessage = "At least one item is required.")]
        [MinLength(1, ErrorMessage = "At least one item is required.")]
        [MaxLength(200, ErrorMessage = "A return cannot have more than 200 items.")]
        public List<ReturnPurchaseItemRequest> Items { get; set; } = new();

        [Required(ErrorMessage = "Enter a reason for the return.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "The reason must be 3 to 500 characters.")]
        public string? Reason { get; set; }

        /// <summary>Money already received back from the supplier.</summary>
        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Refund cannot be negative.")]
        public decimal RefundReceived { get; set; }

        public PaymentMethod? RefundMethod { get; set; }

        /// <summary>How the rest is settled, e.g. "credit against next order".</summary>
        [StringLength(500, ErrorMessage = "The adjustment note cannot exceed 500 characters.")]
        public string? AdjustmentNote { get; set; }
    }

    public class ReturnPurchaseItemRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a purchase line.")]
        public int PurchaseItemId { get; set; }

        [Range(1, 1_000_000, ErrorMessage = "Return quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    public class PurchaseReturnSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? PurchaseId { get; set; }
    }

    public class PurchaseReturnResponse
    {
        public int ReturnId { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal RefundReceived { get; set; }
    }

    public class PurchaseReturnListItemDto
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal RefundReceived { get; set; }
    }

    public class PurchaseReturnDetailDto
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public string? Reason { get; set; }
        public string? CreatedBy { get; set; }

        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public string? SupplierInvoiceNumber { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierGstin { get; set; }
        public bool IsInterState { get; set; }

        public decimal Subtotal { get; set; }
        public decimal CgstTotal { get; set; }
        public decimal SgstTotal { get; set; }
        public decimal IgstTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal RoundOff { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal RefundReceived { get; set; }
        public string? RefundMethod { get; set; }
        public string? AdjustmentNote { get; set; }

        public List<ReturnItemDetailDto> Items { get; set; } = new();
    }

    public class ReturnItemDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? HsnCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }
}