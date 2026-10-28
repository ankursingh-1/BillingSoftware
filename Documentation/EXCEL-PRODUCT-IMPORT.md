# Product Excel import and export

Use this to add many products at once. Existing products are **never overwritten**.

## Columns (Products sheet only)

| Column | Required | Notes |
|---|---|---|
| ProductName | yes | Up to 200 characters. Duplicate names are allowed. |
| SKU | yes | Up to 200 characters. Must be unique. |
| Category | no | Must match an **existing** category name (see the Lookups sheet). |
| Brand | no | Must match an existing brand name. |
| Unit | no | Must match an existing unit name. |
| Tax | no | Existing tax **name** (e.g. GST 18) or the rate number (e.g. 18). Inactive taxes are rejected. |
| HSN | no | 4, 6 or 8 digits. |
| PurchasePrice | yes | 0 or more, at most 2 decimal places. |
| SellingPrice | yes | 0 or more, at most 2 decimal places. |
| OpeningStock | no | Whole number 0 or more. Blank = 0. Posted through the stock ledger as opening stock. |

There is **no barcode, description or IsActive column** — those fields are not on the product.

The **Sample** sheet is an example only and is never imported. Copy rows onto **Products** if you want them.

## Limits

- File type: `.xlsx` only (not CSV, not `.xls`).
- Size: 5 MB.
- Rows: 2,000 product rows on the Products sheet.

## Behaviour

1. Download the template from Products → Import Excel.
2. Fill the Products sheet. Use names from the Lookups sheet for category, brand, unit and tax.
3. Upload. The preview lists valid, duplicate and invalid rows with the original Excel row number.
4. Confirm. **Valid rows are saved in one transaction** (all succeed, or none are written).
5. Duplicate SKUs (in the file or already in the catalogue) are skipped and listed. Stock is not added twice if you retry the same file.
6. Download the error report for failed/skipped rows.

Invalid rows never reach the database. Opening stock uses the same inventory service as a manual product create.

## APIs (authenticated)

- `GET /api/Products/import/template`
- `POST /api/Products/import/preview` (multipart `file`)
- `POST /api/Products/import` (multipart `file`)
- `POST /api/Products/import/error-report` (multipart `file`)
- `GET /api/Products/export`

## Formula injection

Generated Excel files prefix cells that start with `=`, `+`, `-` or `@` so they cannot run as formulas.

## Sample workbook

`Documentation/samples/product-import-test.xlsx` has valid, invalid and duplicate rows on the Products sheet. Import it only on a Verify/copy database.
