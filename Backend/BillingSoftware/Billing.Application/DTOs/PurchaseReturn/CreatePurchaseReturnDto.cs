using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.PurchaseReturn
{
    public class CreatePurchaseReturnDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product.")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }

        [StringLength(50, ErrorMessage = "Reference number cannot exceed 50 characters.")]
        public string? ReferenceNo { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
        public string? Remarks { get; set; }
    }
}