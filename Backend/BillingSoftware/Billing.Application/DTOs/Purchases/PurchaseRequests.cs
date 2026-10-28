using System.ComponentModel.DataAnnotations;
using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Purchases
{
    public class CreatePurchaseRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a supplier.")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Purchase date is required.")]
        public DateTime PurchaseDate { get; set; }

        /// <summary>The supplier's own bill number, for matching against their invoice.</summary>
        [StringLength(50, ErrorMessage = "Supplier invoice number cannot exceed 50 characters.")]
        public string? SupplierInvoiceNumber { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
        public string? Remarks { get; set; }

        [Required(ErrorMessage = "At least one item is required.")]
        [MinLength(1, ErrorMessage = "At least one item is required.")]
        [MaxLength(200, ErrorMessage = "A purchase cannot have more than 200 items.")]
        public List<CreatePurchaseItemRequest> Items { get; set; } = new();

        /// <summary>
        /// When true, each product's catalogue cost price is set to the price on this purchase.
        /// Off by default: sales profit uses the catalogue cost, so changing it is a deliberate choice.
        /// </summary>
        public bool UpdateCatalogueCost { get; set; }

        public Guid? ClientRequestId { get; set; }
    }

    public class CreatePurchaseItemRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a product.")]
        public int ProductId { get; set; }

        [Range(1, 1_000_000, ErrorMessage = "Quantity must be between 1 and 1,000,000.")]
        public int Quantity { get; set; }

        /// <summary>Cost per unit, excluding tax.</summary>
        [Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "Purchase price must be greater than 0.")]
        public decimal PurchasePrice { get; set; }
    }

    public class PurchaseSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SupplierId { get; set; }
    }

    public class PurchaseResponse
    {
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public bool IsDuplicate { get; set; }
    }
}