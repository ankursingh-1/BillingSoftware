using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Sales
{
    public class CreateSaleRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a customer.")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "At least one item is required.")]
        [MinLength(1, ErrorMessage = "At least one item is required.")]
        [MaxLength(200, ErrorMessage = "A sale cannot have more than 200 items.")]
        public List<CreateSaleItemRequest> Items { get; set; } = new();
    }
}
