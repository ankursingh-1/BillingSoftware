using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class PurchaseItem : BaseEntity
{
    public int PurchaseId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }

    /// <summary>Taxable value of the line (quantity x price, before tax).</summary>
    public decimal Total { get; set; }

    // ---- Tax snapshot. Null on legacy rows. ----
    public string? HsnCode { get; set; }
    public string? TaxName { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxableAmount { get; set; }
    public decimal? CgstAmount { get; set; }
    public decimal? SgstAmount { get; set; }
    public decimal? IgstAmount { get; set; }
    public decimal? TaxAmount { get; set; }

    public Purchase Purchase { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
