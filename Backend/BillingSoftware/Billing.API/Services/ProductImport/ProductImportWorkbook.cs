using System.Globalization;
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
        "PurchasePrice", "SellingPrice", "OpeningStock",
        "Barcode", "WarrantyValue", "WarrantyUnit", "RequiresSerial",
        "ProductId", "StockQuantity", "AddStockQuantity", "ReceiptId"
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
        ["stock"] = "OpeningStock",
        ["stockquantity"] = "StockQuantity",
        ["stock quantity"] = "StockQuantity",
        ["stockqty"] = "StockQuantity",
        ["addstockquantity"] = "AddStockQuantity",
        ["add stock quantity"] = "AddStockQuantity",
        ["addstock"] = "AddStockQuantity",
        ["add stock"] = "AddStockQuantity",
        ["productid"] = "ProductId",
        ["product id"] = "ProductId",
        ["receiptid"] = "ReceiptId",
        ["receipt id"] = "ReceiptId",
        ["batchid"] = "ReceiptId",
        ["importbatchid"] = "ReceiptId",
        ["deliveryid"] = "ReceiptId",
        ["barcode"] = "Barcode",
        ["ean"] = "Barcode",
        ["upc"] = "Barcode",
        ["warrantyvalue"] = "WarrantyValue",
        ["warranty value"] = "WarrantyValue",
        ["warranty"] = "WarrantyValue",
        ["warrantyunit"] = "WarrantyUnit",
        ["warranty unit"] = "WarrantyUnit",
        ["requiresserial"] = "RequiresSerial",
        ["requires serial"] = "RequiresSerial",
        ["serial"] = "RequiresSerial"
    };

    public const string PolicyText = ProductImportPlanner.PolicyText;

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
            "SKU*          — up to 200 characters. Unique. Matches an existing product when Product ID and barcode are blank.",
            "Category      — must match an existing category name exactly (see Lookups). Blank on an existing product leaves the current category.",
            "Brand         — must match an existing brand name (see Lookups).",
            "Unit          — must match an existing unit name (see Lookups).",
            "Tax           — existing tax name (e.g. GST 18) or the rate number (e.g. 18). Inactive taxes are rejected.",
            "HSN           — 4, 6 or 8 digits. Leave blank if unknown. Blank does not clear an existing HSN.",
            "PurchasePrice* — rupees, 0 or more, at most 2 decimal places. Required for new products. Blank on an existing product leaves the current price.",
            "SellingPrice*  — rupees, 0 or more, at most 2 decimal places. Required for new products.",
            "OpeningStock   — desired on-hand for existing products (Excel − current stock is added). Blank is NOT zero. Excel never reduces stock.",
            "Barcode        — optional unique shop/EAN code used by POS scan. Match key #2 after Product ID.",
            "WarrantyValue  — optional default length (whole number). Used only as a default on new sales.",
            "WarrantyUnit   — Days, Months or Years. Required when WarrantyValue is set.",
            "RequiresSerial — Yes or No. Serials are stored on warranty registrations, not as serial-level stock.",
            "ProductId      — optional. Trusted match to an existing product. Do not invent IDs for new rows.",
            "StockQuantity  — same as OpeningStock (desired on-hand). Confirm recalculates the difference against live stock.",
            "AddStockQuantity — Mode B: quantity to add (a delivery). Blank or 0 = no add. Do not combine with StockQuantity.",
            "ReceiptId      — optional delivery/batch id for incremental stock. Same id cannot add twice.",
            "",
            "The Sample sheet is an example only and is NEVER imported.",
            "Do not add extra sheets as the import source. Only the Products sheet is read.",
            "Maximum 2,000 product rows and 5 MB.",
            "Retrying the same file (same bytes) returns the original result; stock is not added twice.",
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

    public static byte[] BuildExport(IEnumerable<(int Id, string Name, string Sku, string? Category, string? Brand, string? Unit, string? Tax, string? Hsn, decimal Purchase, decimal Selling, int Stock, string? Barcode, int? WarrantyValue, string? WarrantyUnit, bool RequiresSerial)> products)
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
            WriteText(sheet.Cell(row, 11), p.Barcode);
            if (p.WarrantyValue.HasValue) sheet.Cell(row, 12).Value = p.WarrantyValue.Value;
            WriteText(sheet.Cell(row, 13), p.WarrantyUnit);
            WriteText(sheet.Cell(row, 14), p.RequiresSerial ? "Yes" : "No");
            sheet.Cell(row, 15).Value = p.Id;
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
            if (item.StockQuantity.HasValue) sheet.Cell(row, 10).Value = item.StockQuantity.Value;
            WriteText(sheet.Cell(row, 11), item.Barcode);
            if (item.WarrantyDurationValue.HasValue) sheet.Cell(row, 12).Value = item.WarrantyDurationValue.Value;
            WriteText(sheet.Cell(row, 13), item.WarrantyDurationUnit);
            WriteText(sheet.Cell(row, 14), item.RequiresSerialNumber ? "Yes" : "No");
            if (item.ProductId.HasValue) sheet.Cell(row, 15).Value = item.ProductId.Value;
            if (item.StockQuantity.HasValue) sheet.Cell(row, 16).Value = item.StockQuantity.Value;
            if (item.AddStockQuantity.HasValue) sheet.Cell(row, 17).Value = item.AddStockQuantity.Value;
            WriteText(sheet.Cell(row, 18), item.ReceiptId);
            sheet.Cell(row, 19).Value = item.ExcelRow;
            sheet.Cell(row, 20).Value = item.Status;
            WriteText(sheet.Cell(row, 21), item.Error);
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
            var openingText = CellText(sheet.Cell(r, Col(map, "OpeningStock")));
            var stockQtyText = CellText(sheet.Cell(r, Col(map, "StockQuantity")));
            var addStockText = CellText(sheet.Cell(r, Col(map, "AddStockQuantity")));
            var productIdText = CellText(sheet.Cell(r, Col(map, "ProductId")));
            var receiptId = CellText(sheet.Cell(r, Col(map, "ReceiptId")));
            var barcode = CellText(sheet.Cell(r, Col(map, "Barcode")));
            var warrantyValueText = CellText(sheet.Cell(r, Col(map, "WarrantyValue")));
            var warrantyUnit = CellText(sheet.Cell(r, Col(map, "WarrantyUnit")));
            var requiresSerial = CellText(sheet.Cell(r, Col(map, "RequiresSerial")));

            if (string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(sku) &&
                string.IsNullOrWhiteSpace(purchaseText) &&
                string.IsNullOrWhiteSpace(sellingText) &&
                string.IsNullOrWhiteSpace(openingText) &&
                string.IsNullOrWhiteSpace(stockQtyText) &&
                string.IsNullOrWhiteSpace(addStockText) &&
                string.IsNullOrWhiteSpace(productIdText))
            {
                continue;
            }

            var row = new ProductImportRowDto
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
                Barcode = NullIfEmpty(barcode),
                WarrantyDurationValue = string.IsNullOrWhiteSpace(warrantyValueText)
                    ? null
                    : TryInt(warrantyValueText, out var wv) ? wv : -1,
                WarrantyDurationUnit = NullIfEmpty(warrantyUnit),
                RequiresSerialNumber = ParseYes(requiresSerial),
                RequiresSerialProvided = !string.IsNullOrWhiteSpace(requiresSerial),
                ReceiptId = NullIfEmpty(receiptId)
            };

            ParseAbsoluteStock(openingText, stockQtyText, row);
            ParseOptionalInt(addStockText, v => row.AddStockQuantity = v, () => row.AddStockQuantityUnparsed = true);
            ParseOptionalInt(productIdText, v => row.ProductId = v, () => row.ProductIdUnparsed = true);

            if (!TryDecimal(purchaseText, out _) && !string.IsNullOrWhiteSpace(purchaseText))
                row.Error = "purchase-unparsed";
            if (!TryDecimal(sellingText, out _) && !string.IsNullOrWhiteSpace(sellingText))
                row.Error = (row.Error ?? "") + "selling-unparsed";

            rows.Add(row);
        }

        if (rows.Count > MaxRows)
            throw new InvalidOperationException(
                $"The file has {rows.Count.ToString("N0", CultureInfo.InvariantCulture)} product rows. The limit is {MaxRows.ToString("N0", CultureInfo.InvariantCulture)}. Split it into smaller files.");

        return rows;
    }

    public static List<ProductImportRowDto> Validate(
        IReadOnlyList<ProductImportRowDto> parsed,
        ProductImportCatalog catalog) =>
        ProductImportPlanner.Plan(parsed, catalog);

    public static ProductImportPreviewDto Summarise(List<ProductImportRowDto> rows) =>
        ProductImportPlanner.Summarise(rows);

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
            return false;

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
        {
            if (d != Math.Truncate(d) || d < int.MinValue || d > int.MaxValue)
                return false;
            value = (int)d;
            return true;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static void ParseOptionalInt(string text, Action<int> assign, Action markUnparsed)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (TryInt(text, out var value))
            assign(value);
        else
            markUnparsed();
    }

    private static void ParseAbsoluteStock(string openingText, string stockQtyText, ProductImportRowDto row)
    {
        var openingBlank = string.IsNullOrWhiteSpace(openingText);
        var qtyBlank = string.IsNullOrWhiteSpace(stockQtyText);

        int? opening = null;
        int? qty = null;
        var bad = false;

        if (!openingBlank)
        {
            if (TryInt(openingText, out var value))
                opening = value;
            else
                bad = true;
        }

        if (!qtyBlank)
        {
            if (TryInt(stockQtyText, out var value))
                qty = value;
            else
                bad = true;
        }

        if (opening.HasValue && qty.HasValue && opening.Value != qty.Value)
            bad = true;

        if (bad)
        {
            row.StockQuantityUnparsed = true;
            return;
        }

        row.StockQuantity = qty ?? opening;
    }

    private static bool ParseYes(string value)
    {
        var t = value.Trim();
        return t.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || t.Equals("y", StringComparison.OrdinalIgnoreCase)
            || t.Equals("true", StringComparison.OrdinalIgnoreCase)
            || t.Equals("1");
    }

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
            ["USB cable", "SAMPLE-USB-01", catalog.Categories.Keys.FirstOrDefault() ?? "Electronics", "", "", catalog.Taxes.FirstOrDefault()?.Name ?? "", "8517", 80, 120, 5],
            ["Existing", "EXISTING-1", "", "", "", "", "", 1, 2, 10]
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
