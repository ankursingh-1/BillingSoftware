using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class Brand : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; }
        = new List<Product>();
}