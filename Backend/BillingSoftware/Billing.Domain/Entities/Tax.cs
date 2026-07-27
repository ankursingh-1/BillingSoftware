using Billing.Domain.Common;

namespace Billing.Domain.Entities
{
    public class Tax : SoftDeleteEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}