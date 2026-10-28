using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class SaleItem : BaseEntity
{
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    /// <summary>Unit price (tax-exclusive) charged on this invoice.</summary>
    public decimal SellingPrice { get; set; }

    /// <summary>Taxable value of the line (quantity x price, before tax).</summary>
    public decimal Total { get; set; }

    // ---- Tax snapshot. Null on legacy rows (tax unknown). ----
    public string? HsnCode { get; set; }
    public string? TaxName { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxableAmount { get; set; }
    public decimal? CgstAmount { get; set; }
    public decimal? SgstAmount { get; set; }
    public decimal? IgstAmount { get; set; }
    public decimal? TaxAmount { get; set; }

    /// <summary>
    /// Product cost price at the time of sale (catalogue purchase price).
    /// Null when it was not known; gross profit is then reported as incomplete.
    /// </summary>
    public decimal? CostPrice { get; set; }

    public Sale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
