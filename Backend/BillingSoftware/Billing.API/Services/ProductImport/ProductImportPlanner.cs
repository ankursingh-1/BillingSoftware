using System.Globalization;
using System.Text.RegularExpressions;
using Billing.Application.DTOs.Products;

namespace Billing.API.Services.ProductImport;

/// <summary>
/// Matches Excel rows to catalogue products, detects duplicates and identity
/// conflicts, and plans field updates plus stock movements. No database writes.
/// </summary>
public static class ProductImportPlanner
{
    public const string PolicyText =
        "A barcode or SKU that already exists is a match, not a duplicate: that product is updated. " +
        "Match by barcode, then SKU (whitespace trimmed; Product ID if present). " +
        "New products are added with OpeningStock as opening quantity. " +
        "For existing products OpeningStock is the desired on-hand target: " +
        "only (Excel − current stock) is added through the ledger. Equal quantities skip stock. " +
        "Blank OpeningStock is not zero and leaves stock unchanged. " +
        "Excel never reduces stock — use Inventory → Adjustments. " +
        "Provided product fields are updated; blank optional cells do not clear stored values. " +
        "Duplicate means the same product appears twice in this file, not that it exists in the catalogue. " +
        "Confirm recalculates the difference against live stock. " +
        "The same file is imported once. Applicable rows are one transaction. Maximum 2,000 rows and 5 MB.";

    private static readonly Regex Hsn = new(@"^([0-9]{4}|[0-9]{6}|[0-9]{8})$", RegexOptions.Compiled);

    public static List<ProductImportRowDto> Plan(
        IReadOnlyList<ProductImportRowDto> parsed,
        ProductImportCatalog catalog)
    {
        var byId = catalog.ExistingProducts.ToDictionary(p => p.Id);
        var bySku = new Dictionary<string, ProductImportExisting>(StringComparer.OrdinalIgnoreCase);
        var byBarcode = new Dictionary<string, ProductImportExisting>(StringComparer.OrdinalIgnoreCase);

        foreach (var product in catalog.ExistingProducts)
        {
            AddLookup(bySku, product.Sku, product);
            AddLookup(byBarcode, product.Barcode, product);
        }

        foreach (var sku in catalog.ExistingSkus)
        {
            if (string.IsNullOrWhiteSpace(sku) || bySku.ContainsKey(sku.Trim()))
                continue;

            bySku[sku.Trim()] = new ProductImportExisting
            {
                Id = 0,
                Sku = sku.Trim(),
                Name = sku.Trim()
            };
        }

        foreach (var barcode in catalog.ExistingBarcodes)
        {
            if (string.IsNullOrWhiteSpace(barcode) || byBarcode.ContainsKey(barcode.Trim()))
                continue;

            byBarcode[barcode.Trim()] = new ProductImportExisting
            {
                Id = 0,
                Barcode = barcode.Trim(),
                Sku = barcode.Trim()
            };
        }

        var rows = parsed.Select(Clone).ToList();
        foreach (var row in rows)
            ValidateFields(row, catalog);

        foreach (var row in rows)
            Match(row, byId, bySku, byBarcode);

        MarkFileDuplicates(rows);
        DetectIdentityCollisions(rows);

        foreach (var row in rows)
        {
            if (row.Status is ProductImportStatus.Invalid
                or ProductImportStatus.Conflict
                or ProductImportStatus.Duplicate)
            {
                continue;
            }

            PlanChanges(row, row.MatchedProductId is > 0 ? byId.GetValueOrDefault(row.MatchedProductId.Value) : null, bySku, byBarcode);
        }

        return rows;
    }

    public static ProductImportPreviewDto Summarise(List<ProductImportRowDto> rows)
    {
        var valid = rows.Count(r => r.Status == ProductImportStatus.Valid);
        var invalid = rows.Count(r => r.Status == ProductImportStatus.Invalid);
        var duplicate = rows.Count(r => r.Status == ProductImportStatus.Duplicate);
        var unchanged = rows.Count(r => r.Status == ProductImportStatus.Unchanged);
        var conflict = rows.Count(r => r.Status == ProductImportStatus.Conflict);

        return new ProductImportPreviewDto
        {
            TotalRows = rows.Count,
            Valid = valid,
            Invalid = invalid,
            Duplicate = duplicate,
            Unchanged = unchanged,
            Conflict = conflict,
            CanImport = valid > 0,
            Policy = PolicyText,
            Rows = rows
        };
    }

    public static ProductImportResultDto ToResult(List<ProductImportRowDto> rows, bool replayed = false)
    {
        var preview = Summarise(rows);
        var added = rows.Count(r => r.Status is ProductImportStatus.Added or ProductImportStatus.Imported);
        var stockIncreased = rows.Count(r => r.WillIncreaseStock &&
            r.Status is ProductImportStatus.Added or ProductImportStatus.Imported
                or ProductImportStatus.Updated or ProductImportStatus.StockIncreased);
        var skipped = rows.Count(r =>
            r.Status is ProductImportStatus.Invalid or ProductImportStatus.Duplicate
                or ProductImportStatus.Unchanged or ProductImportStatus.Conflict
                or ProductImportStatus.Skipped or ProductImportStatus.Failed);

        return new ProductImportResultDto
        {
            TotalRows = preview.TotalRows,
            Valid = preview.Valid,
            Invalid = preview.Invalid,
            Duplicate = preview.Duplicate,
            Unchanged = preview.Unchanged,
            Conflict = preview.Conflict,
            CanImport = false,
            Policy = preview.Policy,
            Rows = preview.Rows,
            Added = added,
            Updated = rows.Count(r => r.Status == ProductImportStatus.Updated),
            StockIncreased = stockIncreased,
            Imported = added,
            Skipped = skipped,
            Replayed = replayed
        };
    }

    private static void ValidateFields(ProductImportRowDto copy, ProductImportCatalog catalog)
    {
        copy.Status = ProductImportStatus.Valid;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(copy.Name))
            errors.Add("Product name is required.");
        else if (copy.Name.Length > 200)
            errors.Add("Product name cannot exceed 200 characters.");
        else
            copy.Name = copy.Name.Trim();

        if (string.IsNullOrWhiteSpace(copy.Sku))
            errors.Add("SKU is required.");
        else if (copy.Sku.Length > 200)
            errors.Add("SKU cannot exceed 200 characters.");
        else
            copy.Sku = copy.Sku.Trim();

        if (copy.Error?.Contains("purchase-unparsed") == true)
            errors.Add("Purchase price is required and must be a number.");
        else if (copy.PurchasePrice.HasValue)
        {
            if (copy.PurchasePrice < 0)
                errors.Add("Purchase price cannot be negative.");
            else if (!HasMaxTwoDecimals(copy.PurchasePrice.Value))
                errors.Add("Purchase price can have at most 2 decimal places.");
        }

        if (copy.Error?.Contains("selling-unparsed") == true)
            errors.Add("Selling price is required and must be a number.");
        else if (copy.SellingPrice.HasValue)
        {
            if (copy.SellingPrice < 0)
                errors.Add("Selling price cannot be negative.");
            else if (!HasMaxTwoDecimals(copy.SellingPrice.Value))
                errors.Add("Selling price can have at most 2 decimal places.");
        }

        if (copy.StockQuantityUnparsed)
            errors.Add("StockQuantity / OpeningStock must be a whole number 0 or more, or blank.");
        else if (copy.StockQuantity < 0)
            errors.Add("StockQuantity / OpeningStock cannot be negative.");

        if (copy.AddStockQuantityUnparsed)
            errors.Add("AddStockQuantity must be a whole number 0 or more, or blank.");
        else if (copy.AddStockQuantity < 0)
            errors.Add("AddStockQuantity cannot be negative.");

        if (copy.StockQuantity.HasValue && copy.AddStockQuantity.HasValue)
            errors.Add("Use either StockQuantity (set stock) or AddStockQuantity (add stock), not both.");

        if (copy.ProductIdUnparsed)
            errors.Add("ProductId must be a whole number, or blank.");
        else if (copy.ProductId is <= 0)
            errors.Add("ProductId must be a positive product id.");

        if (copy.Barcode != null && copy.Barcode.Length > 80)
            errors.Add("Barcode cannot exceed 80 characters.");
        else if (copy.Barcode != null)
            copy.Barcode = copy.Barcode.Trim();

        if (!string.IsNullOrWhiteSpace(copy.ReceiptId))
        {
            copy.ReceiptId = copy.ReceiptId.Trim();
            if (copy.ReceiptId.Length > 80)
                errors.Add("ReceiptId cannot exceed 80 characters.");
        }
        else
            copy.ReceiptId = null;

        if (copy.WarrantyDurationValue == -1)
            errors.Add("WarrantyValue must be a whole number.");
        else if (copy.WarrantyDurationValue is < 1 or > 3650)
            errors.Add("WarrantyValue must be between 1 and 3650, or blank.");
        else if (copy.WarrantyDurationValue.HasValue != !string.IsNullOrWhiteSpace(copy.WarrantyDurationUnit))
            errors.Add("Set both WarrantyValue and WarrantyUnit (Days, Months or Years), or leave both blank.");
        else if (!string.IsNullOrWhiteSpace(copy.WarrantyDurationUnit)
                 && !IsWarrantyUnit(copy.WarrantyDurationUnit))
            errors.Add("WarrantyUnit must be Days, Months or Years.");
        else if (!string.IsNullOrWhiteSpace(copy.WarrantyDurationUnit))
            copy.WarrantyDurationUnit = CanonicalWarrantyUnit(copy.WarrantyDurationUnit);

        if (!string.IsNullOrWhiteSpace(copy.Hsn) && !Hsn.IsMatch(copy.Hsn))
            errors.Add("HSN/SAC must be 4, 6 or 8 digits.");

        if (!string.IsNullOrWhiteSpace(copy.Category))
        {
            if (catalog.Categories.TryGetValue(copy.Category, out var id))
                copy.CategoryId = id;
            else
                errors.Add($"Category '{copy.Category}' was not found. Use a name from the Lookups sheet.");
        }

        if (!string.IsNullOrWhiteSpace(copy.Brand))
        {
            if (catalog.Brands.TryGetValue(copy.Brand, out var id))
                copy.BrandId = id;
            else
                errors.Add($"Brand '{copy.Brand}' was not found.");
        }

        if (!string.IsNullOrWhiteSpace(copy.Unit))
        {
            if (catalog.Units.TryGetValue(copy.Unit, out var id))
                copy.UnitId = id;
            else
                errors.Add($"Unit '{copy.Unit}' was not found.");
        }

        if (!string.IsNullOrWhiteSpace(copy.Tax))
        {
            if (!TryMatchTax(copy.Tax, catalog.Taxes, out var taxId, out var taxError))
                errors.Add(taxError!);
            else
                copy.TaxId = taxId;
        }

        copy.Error = null;

        if (errors.Count > 0)
        {
            copy.Status = ProductImportStatus.Invalid;
            copy.Error = string.Join(" ", errors);
        }
        else
        {
            copy.Error = null;
        }
    }

    private static void Match(
        ProductImportRowDto row,
        Dictionary<int, ProductImportExisting> byId,
        Dictionary<string, ProductImportExisting> bySku,
        Dictionary<string, ProductImportExisting> byBarcode)
    {
        ProductImportExisting? byIdMatch = null;
        ProductImportExisting? byBarcodeMatch = null;
        ProductImportExisting? bySkuMatch = null;

        if (row.ProductId is > 0)
            byId.TryGetValue(row.ProductId.Value, out byIdMatch);

        if (!string.IsNullOrWhiteSpace(row.Barcode))
            byBarcode.TryGetValue(row.Barcode, out byBarcodeMatch);

        if (!string.IsNullOrWhiteSpace(row.Sku))
            bySku.TryGetValue(row.Sku, out bySkuMatch);

        if (row.ProductId is > 0 && byIdMatch == null)
        {
            SetConflict(row, $"Product ID {row.ProductId} was not found.");
            return;
        }

        var matches = new List<ProductImportExisting>();
        void Add(ProductImportExisting? p)
        {
            if (p != null && p.Id > 0 && matches.All(m => m.Id != p.Id))
                matches.Add(p);
        }

        Add(byIdMatch);
        Add(byBarcodeMatch);
        Add(bySkuMatch);

        if (matches.Count > 1)
        {
            var parts = new List<string>();
            if (byIdMatch is { Id: > 0 })
                parts.Add($"Product ID {row.ProductId} matches '{byIdMatch.Sku}' (id {byIdMatch.Id})");
            if (byBarcodeMatch is { Id: > 0 })
                parts.Add($"barcode '{row.Barcode}' belongs to '{byBarcodeMatch.Sku}' (id {byBarcodeMatch.Id})");
            if (bySkuMatch is { Id: > 0 })
                parts.Add($"SKU '{row.Sku}' belongs to '{bySkuMatch.Sku}' (id {bySkuMatch.Id})");
            SetConflict(row,
                $"Product identity is ambiguous. Row {row.ExcelRow}: {string.Join("; ", parts)}. Neither was updated.");
            return;
        }

        if (matches.Count == 1)
        {
            row.MatchedProductId = matches[0].Id;
            row.CurrentStock = matches[0].Stock;
            return;
        }

        if (bySkuMatch is { Id: 0 } || byBarcodeMatch is { Id: 0 })
        {
            row.MatchedProductId = 0;
            row.CurrentStock = bySkuMatch?.Stock ?? byBarcodeMatch?.Stock ?? 0;
        }
    }

    private static void MarkFileDuplicates(List<ProductImportRowDto> rows)
    {
        var groups = rows
            .Select((row, index) => (row, index))
            .GroupBy(x => IdentityKey(x.row));

        foreach (var group in groups)
        {
            var members = group.Select(x => x.row).ToList();
            if (members.Count < 2)
                continue;

            var reviewable = members
                .Where(r => r.Status is not ProductImportStatus.Invalid and not ProductImportStatus.Conflict)
                .ToList();
            if (reviewable.Count < 2)
                continue;

            var distinctReceipts = reviewable
                .Select(r => r.ReceiptId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            if (reviewable.All(r => SameInstructions(r, reviewable[0])))
            {
                for (var i = 1; i < reviewable.Count; i++)
                {
                    reviewable[i].Status = ProductImportStatus.Duplicate;
                    reviewable[i].Error =
                        "Duplicate row in this file. The first identical row is kept; this one is skipped.";
                    reviewable[i].Action = "Duplicate / skipped";
                    ClearPlan(reviewable[i]);
                }

                continue;
            }

            var allHaveReceipts = reviewable.All(r => !string.IsNullOrWhiteSpace(r.ReceiptId));
            var onlyStockDiffers = reviewable.All(r => SameProductFields(r, reviewable[0]));
            if (allHaveReceipts && distinctReceipts == reviewable.Count && onlyStockDiffers)
                continue;

            foreach (var row in reviewable)
            {
                SetConflict(row,
                    "Conflicting duplicate rows for the same product. Review the file; nothing was chosen automatically.");
            }
        }
    }

    private static void DetectIdentityCollisions(List<ProductImportRowDto> rows)
    {
        var assignedBarcodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var assignedSkus = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (row.Status is ProductImportStatus.Invalid or ProductImportStatus.Conflict
                or ProductImportStatus.Duplicate)
                continue;

            if (!string.IsNullOrWhiteSpace(row.Barcode))
            {
                if (assignedBarcodes.TryGetValue(row.Barcode, out var other)
                    && other != (row.MatchedProductId ?? -row.ExcelRow))
                {
                    SetConflict(row, "Duplicate barcode in this file.");
                    continue;
                }

                assignedBarcodes[row.Barcode] = row.MatchedProductId ?? -row.ExcelRow;
            }

            if (!string.IsNullOrWhiteSpace(row.Sku))
            {
                if (assignedSkus.TryGetValue(row.Sku, out var other)
                    && other != (row.MatchedProductId ?? -row.ExcelRow)
                    && string.IsNullOrWhiteSpace(row.ReceiptId))
                {
                    // Same new SKU without a shared identity was already grouped.
                }

                assignedSkus[row.Sku] = row.MatchedProductId ?? -row.ExcelRow;
            }
        }
    }

    private static void PlanChanges(
        ProductImportRowDto row,
        ProductImportExisting? existing,
        Dictionary<string, ProductImportExisting> bySku,
        Dictionary<string, ProductImportExisting> byBarcode)
    {
        var isNew = existing == null || existing.Id <= 0;

        if (isNew)
        {
            if (!string.IsNullOrWhiteSpace(row.Sku)
                && bySku.TryGetValue(row.Sku, out var known)
                && known.Id == 0)
            {
                row.Status = ProductImportStatus.Unchanged;
                row.Action = "Unchanged / skipped";
                row.Error = null;
                ClearPlan(row);
                return;
            }

            if (!row.PurchasePrice.HasValue)
            {
                Fail(row, "Purchase price is required for a new product.");
                return;
            }

            if (!row.SellingPrice.HasValue)
            {
                Fail(row, "Selling price is required for a new product.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(row.Barcode)
                && byBarcode.TryGetValue(row.Barcode, out var barcodeOwner)
                && barcodeOwner.Id > 0)
            {
                SetConflict(row,
                    $"Barcode belongs to another product. Row {row.ExcelRow}: '{row.Barcode}' is on '{barcodeOwner.Sku}' (id {barcodeOwner.Id}).");
                return;
            }

            row.WillAddProduct = true;
            row.CurrentStock = 0;
            PlanStock(row, current: 0);
            if (row.Status != ProductImportStatus.Valid)
                return;

            row.Action = row.WillIncreaseStock
                ? $"Add product; opening stock {row.ProposedStockChange}"
                : "Add product";
            row.Status = ProductImportStatus.Valid;
            return;
        }

        if (!string.IsNullOrWhiteSpace(row.Sku)
            && !row.Sku.Equals(existing!.Sku, StringComparison.OrdinalIgnoreCase)
            && bySku.TryGetValue(row.Sku, out var skuOwner)
            && skuOwner.Id > 0
            && skuOwner.Id != existing.Id)
        {
            SetConflict(row,
                $"SKU belongs to another product. Row {row.ExcelRow}: '{row.Sku}' is already on id {skuOwner.Id}.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(row.Barcode)
            && !string.Equals(row.Barcode, existing!.Barcode, StringComparison.OrdinalIgnoreCase)
            && byBarcode.TryGetValue(row.Barcode, out var barcodeTaken)
            && barcodeTaken.Id > 0
            && barcodeTaken.Id != existing.Id)
        {
            SetConflict(row,
                $"Barcode belongs to another product. Row {row.ExcelRow}: '{row.Barcode}' is on '{barcodeTaken.Sku}' (id {barcodeTaken.Id}).");
            return;
        }

        var changes = FieldChanges(row, existing!);
        row.WillUpdateFields = changes.Count > 0;
        row.FieldChanges = changes.Count > 0 ? string.Join("; ", changes) : null;
        row.CurrentStock = existing!.Stock;
        PlanStock(row, existing.Stock);
        if (row.Status is ProductImportStatus.Invalid or ProductImportStatus.Conflict)
            return;

        if (!row.WillUpdateFields && !row.WillIncreaseStock)
        {
            row.Status = ProductImportStatus.Unchanged;
            row.Action = "Unchanged / skipped";
            row.Error = null;
            return;
        }

        var parts = new List<string>();
        if (row.WillUpdateFields)
            parts.Add("Update " + row.FieldChanges);
        if (row.WillIncreaseStock)
            parts.Add("Increase stock");
        row.Action = string.Join("; ", parts);
        row.Status = ProductImportStatus.Valid;
        row.Error = null;
    }

    /// <summary>
    /// OpeningStock is the desired on-hand. Difference = Excel − current.
    /// Does not clear product-field update flags (used again at confirm against live stock).
    /// </summary>
    public static void RecalculateTargetStock(ProductImportRowDto row, int current)
    {
        row.CurrentStock = current;
        PlanStock(row, current, preserveFieldPlan: true);
    }

    private static void PlanStock(ProductImportRowDto row, int current, bool preserveFieldPlan = false)
    {
        row.ProposedStockChange = 0;
        row.WillIncreaseStock = false;

        if (row.AddStockQuantity.HasValue)
        {
            if (row.AddStockQuantity.Value == 0)
                return;

            row.ProposedStockChange = row.AddStockQuantity.Value;
            row.WillIncreaseStock = true;
            return;
        }

        if (!row.StockQuantity.HasValue)
            return;

        var desired = row.StockQuantity.Value;
        if (desired == current)
            return;

        if (desired > current)
        {
            row.ProposedStockChange = desired - current;
            row.WillIncreaseStock = true;
            return;
        }

        var reduction = StockReductionMessage(row, current, desired);
        if (preserveFieldPlan)
        {
            row.Error = reduction;
            row.Action = row.WillUpdateFields
                ? "Update fields; target stock is lower than current stock"
                : "Target stock is lower than current stock";
            if (!row.WillUpdateFields)
                row.Status = ProductImportStatus.Conflict;
            return;
        }

        SetConflict(row, reduction);
        row.Action = "Target stock is lower than current stock";
    }

    private static string StockReductionMessage(ProductImportRowDto row, int current, int desired) =>
        $"Target stock is lower than current stock. Row {row.ExcelRow} {row.Sku}: on hand {current}, Excel {desired}. Stock reduction is not applied. Use Inventory → Adjustments.";

    private static List<string> FieldChanges(ProductImportRowDto row, ProductImportExisting existing)
    {
        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(row.Name)
            && !row.Name.Equals(existing.Name, StringComparison.Ordinal))
            changes.Add("name");

        if (!string.IsNullOrWhiteSpace(row.Sku)
            && !row.Sku.Equals(existing.Sku, StringComparison.OrdinalIgnoreCase))
            changes.Add("SKU");

        if (row.Barcode != null
            && !string.Equals(row.Barcode, existing.Barcode, StringComparison.OrdinalIgnoreCase))
            changes.Add("barcode");

        if (row.CategoryId.HasValue && row.CategoryId != existing.CategoryId)
            changes.Add("category");
        if (row.BrandId.HasValue && row.BrandId != existing.BrandId)
            changes.Add("brand");
        if (row.UnitId.HasValue && row.UnitId != existing.UnitId)
            changes.Add("unit");
        if (row.TaxId.HasValue && row.TaxId != existing.TaxId)
            changes.Add("tax");
        if (row.Hsn != null
            && !string.Equals(row.Hsn, existing.HsnCode, StringComparison.OrdinalIgnoreCase))
            changes.Add("HSN");
        if (row.PurchasePrice.HasValue && row.PurchasePrice.Value != existing.PurchasePrice)
            changes.Add("purchase price");
        if (row.SellingPrice.HasValue && row.SellingPrice.Value != existing.SellingPrice)
            changes.Add("selling price");
        if (row.WarrantyDurationValue.HasValue
            && (row.WarrantyDurationValue != existing.WarrantyDurationValue
                || !string.Equals(row.WarrantyDurationUnit, existing.WarrantyDurationUnit, StringComparison.OrdinalIgnoreCase)))
            changes.Add("warranty default");
        if (row.RequiresSerialProvided && row.RequiresSerialNumber != existing.RequiresSerialNumber)
            changes.Add("serial required");

        return changes;
    }

    private static void AddLookup(
        Dictionary<string, ProductImportExisting> map,
        string? key,
        ProductImportExisting product)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;
        map.TryAdd(key.Trim(), product);
    }

    private static string IdentityKey(ProductImportRowDto row)
    {
        if (row.MatchedProductId is > 0)
            return "id:" + row.MatchedProductId.Value;
        if (row.ProductId is > 0)
            return "pid:" + row.ProductId.Value;
        if (!string.IsNullOrWhiteSpace(row.Barcode))
            return "bc:" + row.Barcode.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(row.Sku))
            return "sku:" + row.Sku.Trim().ToLowerInvariant();
        return "row:" + row.ExcelRow;
    }

    private static bool SameInstructions(ProductImportRowDto a, ProductImportRowDto b) =>
        SameProductFields(a, b)
        && a.StockQuantity == b.StockQuantity
        && a.AddStockQuantity == b.AddStockQuantity
        && string.Equals(a.ReceiptId, b.ReceiptId, StringComparison.OrdinalIgnoreCase);

    private static bool SameProductFields(ProductImportRowDto a, ProductImportRowDto b) =>
        string.Equals(a.Name, b.Name, StringComparison.Ordinal)
        && string.Equals(a.Sku, b.Sku, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Barcode, b.Barcode, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Category, b.Category, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Brand, b.Brand, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Unit, b.Unit, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Tax, b.Tax, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Hsn, b.Hsn, StringComparison.OrdinalIgnoreCase)
        && a.PurchasePrice == b.PurchasePrice
        && a.SellingPrice == b.SellingPrice
        && a.WarrantyDurationValue == b.WarrantyDurationValue
        && string.Equals(a.WarrantyDurationUnit, b.WarrantyDurationUnit, StringComparison.OrdinalIgnoreCase)
        && a.RequiresSerialNumber == b.RequiresSerialNumber
        && a.ProductId == b.ProductId;

    private static void SetConflict(ProductImportRowDto row, string message)
    {
        row.Status = ProductImportStatus.Conflict;
        row.Error = message;
        row.Action = "Conflict / needs review";
        ClearPlan(row);
    }

    private static void Fail(ProductImportRowDto row, string message)
    {
        row.Status = ProductImportStatus.Invalid;
        row.Error = message;
        ClearPlan(row);
    }

    private static void ClearPlan(ProductImportRowDto row)
    {
        row.WillAddProduct = false;
        row.WillUpdateFields = false;
        row.WillIncreaseStock = false;
        row.ProposedStockChange = 0;
    }

    private static bool IsWarrantyUnit(string unit) =>
        unit.Equals("Days", StringComparison.OrdinalIgnoreCase)
        || unit.Equals("Months", StringComparison.OrdinalIgnoreCase)
        || unit.Equals("Years", StringComparison.OrdinalIgnoreCase);

    private static string CanonicalWarrantyUnit(string unit) =>
        char.ToUpperInvariant(unit.Trim()[0]) + unit.Trim()[1..].ToLowerInvariant();

    private static bool HasMaxTwoDecimals(decimal value) =>
        Math.Abs(value * 100m - Math.Round(value * 100m, 0, MidpointRounding.AwayFromZero)) < 0.0000001m;

    private static bool TryMatchTax(string text, List<ProductImportTax> taxes, out int? id, out string? error)
    {
        id = null;
        error = null;
        var trimmed = text.Trim().TrimEnd('%');

        var byName = taxes.Where(t => t.Name.Equals(text.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (byName.Count == 1)
        {
            id = byName[0].Id;
            return true;
        }

        if (byName.Count > 1)
        {
            error = $"More than one tax is named '{text}'.";
            return false;
        }

        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
        {
            var byRate = taxes.Where(t => t.Percentage == rate).ToList();
            if (byRate.Count == 1)
            {
                id = byRate[0].Id;
                return true;
            }

            if (byRate.Count > 1)
            {
                error = $"More than one tax has rate {rate}%. Use the tax name from the Lookups sheet.";
                return false;
            }
        }

        error = $"Tax '{text}' was not found or is inactive.";
        return false;
    }

    public static ProductImportRowDto Clone(ProductImportRowDto row) => new()
    {
        ExcelRow = row.ExcelRow,
        Name = row.Name,
        Sku = row.Sku,
        Category = row.Category,
        Brand = row.Brand,
        Unit = row.Unit,
        Tax = row.Tax,
        Hsn = row.Hsn,
        PurchasePrice = row.PurchasePrice,
        SellingPrice = row.SellingPrice,
        StockQuantity = row.StockQuantity,
        AddStockQuantity = row.AddStockQuantity,
        Barcode = row.Barcode,
        WarrantyDurationValue = row.WarrantyDurationValue,
        WarrantyDurationUnit = row.WarrantyDurationUnit,
        RequiresSerialNumber = row.RequiresSerialNumber,
        RequiresSerialProvided = row.RequiresSerialProvided,
        ProductId = row.ProductId,
        ReceiptId = row.ReceiptId,
        Status = row.Status,
        Error = row.Error,
        Action = row.Action,
        FieldChanges = row.FieldChanges,
        MatchedProductId = row.MatchedProductId,
        CurrentStock = row.CurrentStock,
        ProposedStockChange = row.ProposedStockChange,
        WillAddProduct = row.WillAddProduct,
        WillUpdateFields = row.WillUpdateFields,
        WillIncreaseStock = row.WillIncreaseStock,
        CategoryId = row.CategoryId,
        BrandId = row.BrandId,
        UnitId = row.UnitId,
        TaxId = row.TaxId,
        StockQuantityUnparsed = row.StockQuantityUnparsed,
        AddStockQuantityUnparsed = row.AddStockQuantityUnparsed,
        ProductIdUnparsed = row.ProductIdUnparsed
    };
}
