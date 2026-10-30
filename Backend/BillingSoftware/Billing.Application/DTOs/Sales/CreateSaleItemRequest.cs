using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Sales
{
    public class CreateSaleItemRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a product.")]
        public int ProductId { get; set; }

        [Range(1, 1_000_000, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        /// <summary>
        /// Optional. The price the user saw when building the cart.
        /// It is NEVER used to price the sale: the server always charges the
        /// current catalogue selling price. If supplied and different from the
        /// catalogue price, the request is rejected with 409 so a stale cart
        /// cannot silently be billed at another price. Price overrides are not
        /// supported.
        /// </summary>
        [Range(typeof(decimal), "0", "9999999999999999.99",
            ErrorMessage = "Price cannot be negative.")]
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// When true, register a warranty for this line. When null, the server
        /// registers only if the product has a default warranty, the shop is not
        /// Grocery, and the customer is not Walk-in.
        /// </summary>
        public bool? RegisterWarranty { get; set; }

        [Range(1, 3650)]
        public int? WarrantyDurationValue { get; set; }

        /// <summary>Days, Months or Years. Overrides the product default for this sale only.</summary>
        [StringLength(20)]
        public string? WarrantyDurationUnit { get; set; }

        public DateTime? WarrantyStartDate { get; set; }

        [StringLength(500)]
        public string? WarrantyNotes { get; set; }

        /// <summary>One serial per unit when the product requires serials. Optional otherwise.</summary>
        [MaxLength(100)]
        public List<string>? SerialNumbers { get; set; }
    }
}
