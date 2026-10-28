using System.ComponentModel.DataAnnotations;

namespace Billing.API.Models;

public class ProductDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(200, ErrorMessage = "SKU cannot exceed 200 characters.")]
    public string SKU { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999.99",
        ErrorMessage = "Purchase price cannot be negative.")]
    public decimal PurchasePrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99",
        ErrorMessage = "Selling price cannot be negative.")]
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Opening stock when creating a product. Ignored on update:
    /// stock only changes through purchases, sales, returns and stock adjustments.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; set; }

    /// <summary>HSN (goods) or SAC (services) code: 4, 6 or 8 digits. Printed on tax invoices.</summary>
    [RegularExpression(@"^([0-9]{4}|[0-9]{6}|[0-9]{8})?$",
        ErrorMessage = "HSN/SAC code must be 4, 6 or 8 digits.")]
    public string? HsnCode { get; set; }

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }
    public int? UnitId { get; set; }
    public string? UnitName { get; set; }
    public int? TaxId { get; set; }
    public string? TaxName { get; set; }
    public decimal? TaxPercentage { get; set; }
}
