using System.Globalization;
using System.Text.RegularExpressions;
using Billing.Application.DTOs.Products;
using ClosedXML.Excel;

namespace Billing.API.Services.ProductImport;

/// <summary>
/// Builds and reads the product import workbook. Example rows live on a
/// separate Sample sheet and are never imported unless copied onto Products.
/// </summary>
public static class ProductImportWorkbook
{
    public const int MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxRows = 2000;

    public const string ProductsSheet = "Products";
    public const string SampleSheet = "Sample";
    public const string InstructionsSheet = "Instructions";
    public const string LookupsSheet = "Lookups";

    public static readonly string[] Headers =
    {
        "ProductName", "SKU", "Category", "Brand", "Unit", "Tax", "HSN",
        "PurchasePrice", "SellingPrice", "OpeningStock"
    };

    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["productname"] = "ProductName",
        ["product name"] = "ProductName",
        ["name"] = "ProductName",
        ["sku"] = "SKU",
        ["category"] = "Category",
        ["categoryname"] = "Category",
        ["brand"] = "Brand",
        ["unit"] = "Unit",
        ["tax"] = "Tax",
        ["gst"] = "Tax",
        ["taxname"] = "Tax",
        ["hsn"] = "HSN",
        ["hsncode"] = "HSN",
        ["sac"] = "HSN",
        ["purchaseprice"] = "PurchasePrice",
        ["purchase price"] = "PurchasePrice",
        ["cost"] = "PurchasePrice",
        ["sellingprice"] = "SellingPrice",
        ["selling price"] = "SellingPrice",
        ["price"] = "SellingPrice",
        ["mrp"] = "SellingPrice",
        ["openingstock"] = "OpeningStock",
        ["opening stock"] = "OpeningStock",
        ["stock"] = "OpeningStock"
    };

    private static readonly Regex Hsn = new(@"^([0-9]{4}|[0-9]{6}|[0-9]{8})$", RegexOptions.Compiled);

    public const string PolicyText =
        "Existing products are never overwritten. Rows with a SKU that already exists " +
        "(in the file or in the catalogue) are skipped and listed in the result. " +
        "Invalid rows are reported and not imported. Valid rows are written in one " +
        "transaction: they all succeed or none are saved. Opening stock goes through " +
        "the stock ledger. There is no barcode or description column — those fields " +
        "are not on the product. Duplicate product names are allowed; SKU must be unique. " +
        "Maximum 2,000 data rows and 5 MB per file.";

    public static byte[] BuildTemplate(ProductImportCatalog catalog)
    {
        using var workbook = new XLWorkbook();
        var products = workbook.AddWorksheet(ProductsSheet);
        WriteHeader(products);
        products.SheetView.FreezeRows(1);
        products.Columns().AdjustToContents();

        var sample = workbook.AddWorksheet(SampleSheet);
        WriteHeader(sample);
        sample.Cell(2, 1).Value = "DEMO USB cable";
        sample.Cell(2, 2).Value = "DEMO-USB-01";
        sample.Cell(2, 3).Value = catalog.Categories.Keys.FirstOrDefault() ?? "Electronics";
        sample.Cell(2, 7).Value = "8517";
        sample.Cell(2, 8).Value = 80;
        sample.Cell(2, 9).Value = 120;
        sample.Cell(2, 10).Value = 10;
        sample.Cell(3, 1).Value = "DEMO HDMI cable";
        sample.Cell(3, 2).Value = "DEMO-HDMI-01";
        sample.Cell(3, 8).Value = 150;
        sample.Cell(3, 9).Value = 220;
        sample.Cell(3, 10).Value = 0;
        sample.Columns().AdjustToContents();

        var instructions = workbook.AddWorksheet(InstructionsSheet);
        var lines = new[]
        {
            "Product import — fill the Products sheet, then upload it in Billing Software.",
            "",
            "Required columns are marked * in the header row.",
            "ProductName*  — up to 200 characters. Duplicate names are allowed.",
            "SKU*          — up to 200 characters. Must be unique. Existing SKUs are skipped, never overwritten.",
            "Category      — must match an existing category name exactly (see Lookups).",
            "Brand         — must match an existing brand name (see Lookups).",
            "Unit          — must match an existing unit name (see Lookups).",
            "Tax           — existing tax name (e.g. GST 18) or the rate number (e.g. 18). Inactive taxes are rejected.",
            "HSN           — 4, 6 or 8 digits. Leave blank if unknown.",
            "PurchasePrice* — rupees, 0 or more, at most 2 decimal places.",
            "SellingPrice*  — rupees, 0 or more, at most 2 decimal places.",
            "OpeningStock   — whole number 0 or more. Added through the stock ledger as opening stock. Leave blank for 0.",
            "",
            "The Sample sheet is an example only and is NEVER imported.",
            "Do not add extra sheets as the import source. Only the Products sheet is read.",
            "Maximum 2,000 product rows and 5 MB.",
            "Retrying the same file is safe: products already imported (same SKU) are skipped, so stock is not added twice.",
            "",
            PolicyText
        };

        for (var i = 0; i < lines.Length; i++)
            instructions.Cell(i + 1, 1).Value = lines[i];

        instructions.Column(1).Width = 110;
        instructions.Cell(1, 1).Style.Font.Bold = true;

        var lookups = workbook.AddWorksheet(LookupsSheet);
        lookups.Cell(1, 1).Value = "Categories";
        lookups.Cell(1, 2).Value = "Brands";
        lookups.Cell(1, 3).Value = "Units";
        lookups.Cell(1, 4).Value = "Taxes (name)";
        lookups.Cell(1, 5).Value = "Taxes (rate %)";
        lookups.Range(1, 1, 1, 5).Style.Font.Bold = true;

        var row = 2;
        foreach (var name in catalog.Categories.Keys.OrderBy(x => x))
            lookups.Cell(row++, 1).Value = name;

        row = 2;
        foreach (var name in catalog.Brands.Keys.OrderBy(x => x))
            lookups.Cell(row++, 2).Value = name;

        row = 2;
        foreach (var name in catalog.Units.Keys.OrderBy(x => x))
            lookups.Cell(row++, 3).Value = name;

        row = 2;
        foreach (var tax in catalog.Taxes.OrderBy(t => t.Name))
        {
            lookups.Cell(row, 4).Value = tax.Name;
            lookups.Cell(row, 5).Value = tax.Percentage;
            row++;
        }

        lookups.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] BuildExport(IEnumerable<(string Name, string Sku, string? Category, string? Brand, string? Unit, string? Tax, string? Hsn, decimal Purchase, decimal Selling, int Stock)> products)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Products");
        WriteHeader(sheet, requiredMarks: false);

        var row = 2;
        foreach (var p in products)
        {
            WriteText(sheet.Cell(row, 1), p.Name);
            WriteText(sheet.Cell(row, 2), p.Sku);
            WriteText(sheet.Cell(row, 3), p.Category);
            WriteText(sheet.Cell(row, 4), p.Brand);
            WriteText(sheet.Cell(row, 5), p.Unit);
            WriteText(sheet.Cell(row, 6), p.Tax);
            WriteText(sheet.Cell(row, 7), p.Hsn);
            sheet.Cell(row, 8).Value = p.Purchase;
            sheet.Cell(row, 8).Style.NumberFormat.Format = "0.00";
            sheet.Cell(row, 9).Value = p.Selling;
            sheet.Cell(row, 9).Style.NumberFormat.Format = "0.00";
            sheet.Cell(row, 10).Value = p.Stock;
            row++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] BuildResultReport(IEnumerable<ProductImportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import results");
        var headers = Headers.Concat(new[] { "ExcelRow", "Status", "Error" }).ToArray();

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var item in rows)
        {
            WriteText(sheet.Cell(row, 1), item.Name);
            WriteText(sheet.Cell(row, 2), item.Sku);
            WriteText(sheet.Cell(row, 3), item.Category);
            WriteText(sheet.Cell(row, 4), item.Brand);
            WriteText(sheet.Cell(row, 5), item.Unit);
            WriteText(sheet.Cell(row, 6), item.Tax);
            WriteText(sheet.Cell(row, 7), item.Hsn);
            if (item.PurchasePrice.HasValue) sheet.Cell(row, 8).Value = item.PurchasePrice.Value;
            if (item.SellingPrice.HasValue) sheet.Cell(row, 9).Value = item.SellingPrice.Value;
            sheet.Cell(row, 10).Value = item.OpeningStock;
            sheet.Cell(row, 11).Value = item.ExcelRow;
            sheet.Cell(row, 12).Value = item.Status;
            WriteText(sheet.Cell(row, 13), item.Error);
            row++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static void EnsureSupportedFile(byte[] bytes, string? fileName)
    {
        if (bytes.Length == 0)
            throw new InvalidOperationException("The file is empty.");

        if (bytes.Length > MaxFileBytes)
            throw new InvalidOperationException("The file is larger than 5 MB.");

        if (bytes.Length < 4 || bytes[0] != (byte)'P' || bytes[1] != (byte)'K')
            throw new InvalidOperationException("Upload an .xlsx Excel file (not CSV or the older .xls format).");

        var name = fileName ?? "";
        if (name.Length > 0 && !name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Upload an .xlsx Excel file.");
    }

    public static List<ProductImportRowDto> ParseProductsSheet(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheets.FirstOrDefault(s =>
            s.Name.Equals(ProductsSheet, StringComparison.OrdinalIgnoreCase));

        if (sheet == null)
            throw new InvalidOperationException(
                "The workbook has no 'Products' sheet. Download the template and fill that sheet. The Sample sheet is not imported.");

        var used = sheet.RangeUsed();
        if (used == null)
            return new List<ProductImportRowDto>();

        var headerRow = used.FirstRow().RowNumber();
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in sheet.Row(headerRow).CellsUsed())
        {
            var raw = cell.GetString().Trim().TrimEnd('*');
            if (HeaderAliases.TryGetValue(raw.Replace(" ", ""), out var canonical) ||
                HeaderAliases.TryGetValue(raw, out canonical))
            {
                if (!map.ContainsKey(canonical))
                    map[canonical] = cell.Address.ColumnNumber;
            }
        }

        if (!map.ContainsKey("ProductName") || !map.ContainsKey("SKU"))
            throw new InvalidOperationException(
                "The Products sheet must have ProductName and SKU columns. Download the template.");

        var rows = new List<ProductImportRowDto>();
        var last = used.LastRow().RowNumber();

        for (var r = headerRow + 1; r <= last; r++)
        {
            var name = CellText(sheet.Cell(r, map["ProductName"]));
            var sku = CellText(sheet.Cell(r, Col(map, "SKU")));
            var category = CellText(sheet.Cell(r, Col(map, "Category")));
            var brand = CellText(sheet.Cell(r, Col(map, "Brand")));
            var unit = CellText(sheet.Cell(r, Col(map, "Unit")));
            var tax = CellText(sheet.Cell(r, Col(map, "Tax")));
            var hsn = CellText(sheet.Cell(r, Col(map, "HSN")));
            var purchaseText = CellText(sheet.Cell(r, Col(map, "PurchasePrice")));
            var sellingText = CellText(sheet.Cell(r, Col(map, "SellingPrice")));
            var stockText = CellText(sheet.Cell(r, Col(map, "OpeningStock")));

            if (string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(sku) &&
                string.IsNullOrWhiteSpace(purchaseText) &&
                string.IsNullOrWhiteSpace(sellingText))
            {
                continue;
            }

            rows.Add(new ProductImportRowDto
            {
                ExcelRow = r,
                Name = name,
                Sku = sku,
                Category = NullIfEmpty(category),
                Brand = NullIfEmpty(brand),
                Unit = NullIfEmpty(unit),
                Tax = NullIfEmpty(tax),
                Hsn = NullIfEmpty(hsn),
                PurchasePrice = TryDecimal(purchaseText, out var purchase) ? purchase : null,
                SellingPrice = TryDecimal(sellingText, out var selling) ? selling : null,
                OpeningStock = TryInt(stockText, out var stock) ? stock : int.MinValue
            });

            // Stash unparsed price/stock text on Error temporarily? Better keep raw via sentinel.
            if (!TryDecimal(purchaseText, out _) && !string.IsNullOrWhiteSpace(purchaseText))
                rows[^1].Error = "purchase-unparsed";
            if (!TryDecimal(sellingText, out _) && !string.IsNullOrWhiteSpace(sellingText))
                rows[^1].Error = (rows[^1].Error ?? "") + "selling-unparsed";
            if (!string.IsNullOrWhiteSpace(stockText) && !TryInt(stockText, out _))
                rows[^1].OpeningStock = int.MinValue;
        }

        if (rows.Count > MaxRows)
            throw new InvalidOperationException(
                $"The file has {rows.Count.ToString("N0", CultureInfo.InvariantCulture)} product rows. The limit is {MaxRows.ToString("N0", CultureInfo.InvariantCulture)}. Split it into smaller files.");

        return rows;
    }

    public static List<ProductImportRowDto> Validate(
        IReadOnlyList<ProductImportRowDto> parsed,
        ProductImportCatalog catalog)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ProductImportRowDto>(parsed.Count);

        foreach (var row in parsed)
        {
            var copy = Clone(row);
            copy.Status = ProductImportStatus.Valid;
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(copy.Name))
                errors.Add("Product name is required.");
            else if (copy.Name.Length > 200)
                errors.Add("Product name cannot exceed 200 characters.");

            if (string.IsNullOrWhiteSpace(copy.Sku))
                errors.Add("SKU is required.");
            else if (copy.Sku.Length > 200)
                errors.Add("SKU cannot exceed 200 characters.");

            if (row.Error?.Contains("purchase-unparsed") == true || !copy.PurchasePrice.HasValue)
                errors.Add("Purchase price is required and must be a number.");
            else if (copy.PurchasePrice < 0)
                errors.Add("Purchase price cannot be negative.");
            else if (!HasMaxTwoDecimals(copy.PurchasePrice.Value))
                errors.Add("Purchase price can have at most 2 decimal places.");

            if (row.Error?.Contains("selling-unparsed") == true || !copy.SellingPrice.HasValue)
                errors.Add("Selling price is required and must be a number.");
            else if (copy.SellingPrice < 0)
                errors.Add("Selling price cannot be negative.");
            else if (!HasMaxTwoDecimals(copy.SellingPrice.Value))
                errors.Add("Selling price can have at most 2 decimal places.");

            if (copy.OpeningStock == int.MinValue)
            {
                errors.Add("Opening stock must be a whole number 0 or more.");
                copy.OpeningStock = 0;
            }
            else if (copy.OpeningStock < 0)
                errors.Add("Opening stock cannot be negative.");

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

            if (!string.IsNullOrWhiteSpace(copy.Sku))
            {
                if (!seen.Add(copy.Sku))
                {
                    copy.Status = ProductImportStatus.Duplicate;
                    copy.Error = "Duplicate SKU in this file. The first row with this SKU is kept; this one is skipped.";
                }
                else if (catalog.ExistingSkus.Contains(copy.Sku))
                {
                    copy.Status = ProductImportStatus.Duplicate;
                    copy.Error = "SKU already exists. Existing products are not overwritten.";
                }
            }

            if (errors.Count > 0)
            {
                var joined = string.Join(" ", errors);
                if (copy.Status == ProductImportStatus.Duplicate)
                    copy.Error = joined + " " + copy.Error;
                else
                {
                    copy.Status = ProductImportStatus.Invalid;
                    copy.Error = joined;
                }
            }
            else if (copy.Status == ProductImportStatus.Valid)
            {
                copy.Error = null;
            }

            result.Add(copy);
        }

        return result;
    }

    public static ProductImportPreviewDto Summarise(List<ProductImportRowDto> rows)
    {
        var valid = rows.Count(r => r.Status == ProductImportStatus.Valid);
        var invalid = rows.Count(r => r.Status == ProductImportStatus.Invalid);
        var duplicate = rows.Count(r => r.Status == ProductImportStatus.Duplicate);

        return new ProductImportPreviewDto
        {
            TotalRows = rows.Count,
            Valid = valid,
            Invalid = invalid,
            Duplicate = duplicate,
            CanImport = valid > 0,
            Policy = PolicyText,
            Rows = rows
        };
    }

    private static void WriteHeader(IXLWorksheet sheet, bool requiredMarks = true)
    {
        for (var i = 0; i < Headers.Length; i++)
        {
            var required = Headers[i] is "ProductName" or "SKU" or "PurchasePrice" or "SellingPrice";
            sheet.Cell(1, i + 1).Value = required && requiredMarks ? Headers[i] + "*" : Headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
            sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            sheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }
    }

    private static int Col(Dictionary<string, int> map, string name) =>
        map.TryGetValue(name, out var col) ? col : 99;

    private static string CellText(IXLCell cell)
    {
        if (cell == null || cell.IsEmpty())
            return "";

        try
        {
            switch (cell.DataType)
            {
                case XLDataType.Number:
                    var n = cell.GetDouble();
                    if (Math.Abs(n - Math.Truncate(n)) < 1e-9 && Math.Abs(n) < 1e12)
                        return ((long)Math.Truncate(n)).ToString(CultureInfo.InvariantCulture);
                    return n.ToString("0.##########", CultureInfo.InvariantCulture);
                case XLDataType.Boolean:
                    return cell.GetBoolean() ? "true" : "false";
                default:
                    return cell.GetString().Trim();
            }
        }
        catch
        {
            return cell.GetFormattedString().Trim();
        }
    }

    private static bool TryDecimal(string text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("en-IN"), out value);
    }

    private static bool TryInt(string text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return true;
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
        {
            if (d != Math.Truncate(d) || d < int.MinValue || d > int.MaxValue)
                return false;
            value = (int)d;
            return true;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

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

    private static ProductImportRowDto Clone(ProductImportRowDto row) => new()
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
        OpeningStock = row.OpeningStock,
        Status = row.Status,
        Error = row.Error
    };

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Stops spreadsheet formula injection in generated files.</summary>
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@\t\r".Contains(trimmed[0]))
            return "'" + value;

        return value;
    }

    /// <summary>
    /// Guide file with valid, invalid and duplicate rows on the Products sheet.
    /// The Sample sheet is still ignored by the importer.
    /// </summary>
    public static byte[] BuildGuideWorkbook(ProductImportCatalog catalog)
    {
        using var input = new MemoryStream(BuildTemplate(catalog));
        using var workbook = new XLWorkbook(input);

        var products = workbook.Worksheet(ProductsSheet);
        object[][] rows =
        [
            ["USB cable", "SAMPLE-USB-01", catalog.Categories.Keys.FirstOrDefault() ?? "Electronics", "", "", catalog.Taxes.FirstOrDefault()?.Name ?? "", "8517", 80, 120, 5],
            ["HDMI cable", "SAMPLE-HDMI-01", "", "", "", "", "", 150, 220, 0],
            ["", "SAMPLE-NONAME", "", "", "", "", "", 10, 20, 0],
            ["Negative price", "SAMPLE-NEG-01", "", "", "", "", "", -1, 20, 0],
            ["Duplicate of first", "SAMPLE-USB-01", "", "", "", "", "", 1, 2, 0],
            ["Already in catalogue", "EXISTING-1", "", "", "", "", "", 1, 2, 0]
        ];

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var value = rows[r][c];
                if (value is int i) products.Cell(r + 2, c + 1).Value = i;
                else if (value is decimal d) products.Cell(r + 2, c + 1).Value = d;
                else products.Cell(r + 2, c + 1).Value = value?.ToString() ?? "";
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static void WriteText(IXLCell cell, string? value)
    {
        var text = Sanitize(value);
        cell.Value = text;
        cell.Style.NumberFormat.Format = "@";
    }
}
