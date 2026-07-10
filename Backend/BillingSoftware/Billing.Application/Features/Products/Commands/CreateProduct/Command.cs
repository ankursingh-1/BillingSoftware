using MediatR;

namespace Billing.Application.Features.Products.Commands.CreateProduct
{
    public class Command : IRequest<int>
    {
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int Stock { get; set; }
    }
}