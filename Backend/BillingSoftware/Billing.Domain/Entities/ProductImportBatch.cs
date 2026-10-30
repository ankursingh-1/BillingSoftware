using Billing.Domain.Common;

namespace Billing.Domain.Entities;

/// <summary>
/// One uploaded Excel file. The file hash is unique so a repeated upload
/// returns the original result instead of adding stock twice.
/// </summary>
public class ProductImportBatch : AuditableEntity
{
    public string FileHash { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string ResultJson { get; set; } = string.Empty;
}

/// <summary>
/// One incremental (or set-stock) receipt applied during an import.
/// Unique per product + receipt key so the same delivery cannot post twice.
/// </summary>
public class ProductImportReceipt : AuditableEntity
{
    public int BatchId { get; set; }
    public ProductImportBatch Batch { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ReceiptKey { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
