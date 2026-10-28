using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.StockAdjustment
{
    public enum StockAdjustmentReason
    {
        /// <summary>Count correction. Can increase or decrease stock.</summary>
        Correction = 1,
        Damaged = 2,
        Expired = 3,
        Lost = 4
    }

    public class CreateStockAdjustmentDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product.")]
        public int ProductId { get; set; }

        [Range(1, 1_000_000, ErrorMessage = "Quantity must be between 1 and 1,000,000.")]
        public int Quantity { get; set; }

        public bool IncreaseStock { get; set; }

        [EnumDataType(typeof(StockAdjustmentReason), ErrorMessage = "Select a valid reason.")]
        public StockAdjustmentReason Reason { get; set; } = StockAdjustmentReason.Correction;

        [Required(ErrorMessage = "Explain why the stock is being adjusted.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Remarks must be 3 to 500 characters.")]
        public string? Remarks { get; set; }
    }
}