# Product Excel import and export

Use this to add many products, update catalogue fields, and post stock through the ledger. Preview first; confirm applies the applicable rows in one transaction.

## Columns (Products sheet only)

| Column | Required | Notes |
|---|---|---|
| ProductName | yes | Up to 200 characters. Duplicate names are allowed. Blank optional cells on an **existing** product do not clear stored values. |
| SKU | yes | Up to 200 characters. Unique. Match key #3 (after Product ID and barcode). |
| Category | no | Must match an **existing** category name (see the Lookups sheet). |
| Brand | no | Must match an existing brand name. |
| Unit | no | Must match an existing unit name. |
| Tax | no | Existing tax **name** (e.g. GST 18) or the rate number (e.g. 18). Inactive taxes are rejected. |
| HSN | no | 4, 6 or 8 digits. |
| PurchasePrice | new products | 0 or more, at most 2 decimal places. Required for **new** products. Blank on an existing product leaves the current price (and does not rewrite sale-line `CostPrice`). |
| SellingPrice | new products | Same rules as purchase price. |
| OpeningStock | no | **Desired on-hand for existing products.** Alias of `Stock` / `StockQuantity`. Whole number 0 or more. Blank is **not** zero and does not change stock. Only the difference from current database stock is posted. |
| Barcode | no | Optional unique scan code (up to 80). Match key #2. |
| WarrantyValue | no | Optional default length (1–3650). Used only as a default on **new** sales. |
| WarrantyUnit | no | `Days`, `Months` or `Years`. Required when WarrantyValue is set. |
| RequiresSerial | no | `Yes` or `No`. Serials are stored on warranty registrations, not as serial-level stock. Blank on an existing product leaves the current flag. |
| ProductId | no | Trusted match to an existing product (match key #1). Do not invent IDs for new rows. Export fills this. |
| StockQuantity | no | **Mode A — set stock.** Same meaning as OpeningStock. If both columns are present they must agree. |
| AddStockQuantity | no | **Mode B — add stock.** Delivery quantity to add. Blank or 0 = no movement. Do not combine with Mode A on the same row. |
| ReceiptId | no | Optional delivery/batch id for incremental stock. The same product + receipt cannot add twice. Distinct receipts on the same product are allowed. |

There is **no description or IsActive column**. Changing a product's warranty default later does **not** rewrite warranties already saved on invoices. OpeningStock / StockQuantity is **not** treated as incremental stock.

The **Sample** sheet is an example only and is never imported. Copy rows onto **Products** if you want them.

## Stock modes

### Mode A — target stock (`OpeningStock` / `Stock` / `StockQuantity`)

The cell is the **desired on-hand quantity**, not a quantity to add repeatedly.

Stock difference = imported OpeningStock − current database stock.

| On hand | Excel | Result |
|---|---|---|
| 10 | 20 | Increase by 10 through the stock ledger (`Adjustment`). Final stock 20. |
| 50 | 50 | No stock movement. The product is skipped when no other fields changed. |
| 30 | 40 | Increase by 10. Final stock 40. |
| 100 | 100 | Skipped (unchanged). |
| 10 | 5 | **Flagged:** "Target stock is lower than current stock" (row, SKU, on hand, Excel). Use Inventory → Adjustments. |
| 10 | 0 (explicit) | **Flagged** (a reduction to zero). |
| 10 | blank | Existing stock is preserved. Blank is not zero. |
| (new) | 5 | Product created with opening stock 5 (`OpeningStock` ledger type). |

Confirm **recalculates the difference against live stock** (row lock on SQL Server, then reload) so a sale or another import between preview and confirm cannot double-add. A repeated upload of the same file does not create another movement.

### Mode B — incremental stock (`AddStockQuantity`)

The cell is a delivery to add.

| On hand | Excel | Result |
|---|---|---|
| 10 | 5 | Final stock 15. Ledger `Adjustment` (existing) or `OpeningStock` (new product). |
| 10 | 0 | No movement. |
| 10 | blank | No movement. |

Do not put Mode A and Mode B on the same row.

## Product matching

Whitespace is trimmed. Meaningful barcode/SKU characters are not stripped.

An imported barcode or SKU that already exists **matches that product**. It is not a duplicate. Eligible fields and the OpeningStock difference are applied.

1. Exact Product ID, when the cell is a known product.
2. Exact barcode, when both values are present.
3. Exact SKU, when both values are present.
4. Otherwise the row is a new-product candidate.

If barcode points at one existing product and SKU (or Product ID) at another, the row is an **identity conflict** ("Product identity is ambiguous") naming both identities. Neither product is updated or merged. Names that merely look similar are never merged.

A barcode that already belongs to a different product is reported as **"Barcode belongs to another product."** A SKU that already belongs to a different product is **"SKU belongs to another product."** Unchanged catalogue rows are **Unchanged / skipped**, not errors.

**Duplicate** means the same product appears more than once **in this Excel file**. It does not mean “this SKU/barcode already exists in the catalogue”.

Barcode uniqueness follows the existing filtered unique index on `Products.Barcode`.

## Duplicates in the file

Detected before any database write.

- Identical product fields and stock instructions: the first row is kept; later rows are **Duplicate / skipped**.
- Same product with different details or different incremental quantities and no distinct `ReceiptId`: **Conflict / needs review**. Nothing is chosen automatically.
- Same product with distinct `ReceiptId` values: independent deliveries, applied once each.

A repeated upload of the **same file bytes** returns the original result (`replayed`). Stock is not added twice. That uses `ProductImportBatches.FileHash` (SHA-256) plus a per-process gate. Ledger `ReferenceNo` and `ProductImportReceipts.ReceiptKey` are extra guards.

## Change detection

When a cell is provided, it is compared with the database. Blank optional cells mean “not provided”.

Reported per row: Added, Updated, Stock increased, Unchanged / skipped, Duplicate / skipped, Conflict / needs review, Invalid / failed.

A field update and a stock increase can happen together.

## Preview and confirm

1. Download the template from Products → Import Excel.
2. Fill the Products sheet. Use names from the Lookups sheet.
3. Upload. The preview lists every row with on-hand stock, Excel quantity, proposed change, duplicates, conflicts and validation errors.
4. Confirm. **Applicable rows are saved in one transaction** (all succeed, or none are written). OpeningStock differences are recalculated against the latest on-hand before the ledger is written.
5. The result counts products added, updated, stock increased, unchanged skipped, duplicate rows skipped, conflicts, invalid and failed rows.
6. Download the error report for invalid, duplicate, conflict and failed rows.

Stock is never changed merely because a file was selected. Reductions are never applied by this import. Historical `SaleItems.CostPrice` is not rewritten when catalogue prices change.

## Limits

- File type: `.xlsx` only (not CSV, not `.xls`).
- Size: 5 MB.
- Rows: 2,000 product rows on the Products sheet.

## APIs (authenticated)

- `GET /api/Products/import/template`
- `POST /api/Products/import/preview` (multipart `file`)
- `POST /api/Products/import` (multipart `file`)
- `POST /api/Products/import/error-report` (multipart `file`)
- `GET /api/Products/export`

## Formula injection

Generated Excel files prefix cells that start with `=`, `+`, `-` or `@` so they cannot run as formulas.

## Remaining limitations

- Excel import is not a purchase receipt: it does not create suppliers, purchase invoices, or cost snapshots. Use Purchases when you need costing.
- Updating `PurchasePrice` does not rewrite historical `SaleItems.CostPrice`.
- Stock reductions require Inventory → Adjustments.
- `AddProductImportBatches` migration is **prepared, not applied** to live `BillingSoftwareDb`. Until it is applied, file-hash replay falls back to ledger reference matching (same file still will not double-add when SKU/barcode match and absolute stock is unchanged). Apply the migration to Verify (or live, after backup + approval) for full result replay.

## Sample workbook

`Documentation/samples/product-import-test.xlsx` has valid, invalid and duplicate rows on the Products sheet. Import it only on a Verify/copy database.
