using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.StockAdjustment
{
    public class CreateStockAdjustmentDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product.")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }

        public bool IncreaseStock { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
        public string? Remarks { get; set; }
    }
}