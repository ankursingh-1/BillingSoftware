# Demo data

Demo records are labelled `DEMO` so they are obvious in lists and reports. They are **not** created automatically, and they are **disabled in Production**.

## Environments (no secrets)

| What | URL | Database |
|---|---|---|
| Verification UI | `http://localhost:5177` (`VITE_API_URL=http://localhost:7190/api`) | `BillingSoftware_Verify` |
| Verification API | `http://localhost:7190` (`Start-TestApi.ps1`) | `BillingSoftware_Verify` (confirmed 2026-10-29 via SQL `DB_NAME()` on the process connection; the port number alone is not proof) |
| Visual Studio UI | `http://localhost:5173` | User Secrets `DefaultConnection` → `BillingSoftwareDb` |
| Visual Studio API | `https://localhost:7179` | `BillingSoftwareDb` (do not apply warranty migration without backup + approval) |

Warranty tables exist on **Verify only**. A new API binary pointed at `BillingSoftwareDb` will omit warranty metrics (dashboard still loads) and cannot register warranties until that migration is approved.

## Dashboard / API errors

- A genuine `/api/Dashboard/summary` failure still shows **Unable to load dashboard data** (not fake zeros).
- Expiring warranties, recent lists, low stock, top products, and the chart load independently. One of those failing does not blank the totals.
- If summary returns 500, inspect the API log. Do not treat an empty shop as an error: totals of ₹0 with HTTP 200 are real empty data.
- Invoice payment status comes from saved payment rows (voids excluded). After **Record payment**, the invoice reloads from the API. **Partially Paid** means a valid payment exists and a balance remains.

## Enable and seed

1. Use a **copy** of the database (for example `BillingSoftware_Verify`), never the live shop database.
2. In User Secrets or environment variables for that API instance:

```
Demo__Enabled=true
```

Do **not** set `Demo:AllowInProduction` except on a disposable production-like copy.

3. Start the API against that copy.
4. Sign in as an **Admin**.
5. Either click **Settings → Seed demo records**, or `POST /api/Demo/seed`.

The call is refused unless `Demo:Enabled` is true. It is safe to repeat. Existing DEMO SKUs and mobile numbers are reused; extra invoices are not created again. Do not run this against the live shop database.

## What is created

Fictional **grocery + Samsung** sample shop. Prices are **demo figures**, not market quotes. Test GSTINs `36AAAAA0000A1Z5` / `27AAAAA0000A1Z5` are **not** real registrations. Internal barcodes are `DEMO-BC-{SKU}` — not manufacturer EANs.

Stock is created as **opening stock** on the product (ledger type OpeningStock). The seed does **not** also post a purchase for the same quantity, so stock is not doubled. Cost snapshots on new sales use the product’s recorded purchase price (last-cost). Quantity is a **whole packet/piece** (a “1 kg” line is one packed bag).

| Record | What it shows |
|---|---|
| Categories Grocery, Packaged Food, Beverages, Rice & Pulses, Mobile Phones, Mobile Accessories | Filters |
| Brands Samsung, Aashirvaad, Tata, Fortune, Parle, Britannia, Amul | Product forms (Samsung reused if it already exists) |
| Units Piece / Packet / Kilogram / Litre | Integer qty only |
| `GRC-RICE-001` … `GRC-WATER-001` | Ten grocery lines, GST 5% demo rate, `DEMO-BC-GRC-…` |
| `SAM-A06-464` … `SAM-S24FE-8128` | Five Samsung phones, GST 18%, 1-year warranty default, serial required |
| `SAM-CHARGER-001`, `SAM-CABLE-001`, `SAM-COVER-001`, `SAM-GLASS-001` | Accessories; charger has a 6-month default warranty |
| Walk-in Customer `9000000000` | Counter sales (**Walk-in** on New Sale) |
| `DEMO North Retail` (state 36) / `DEMO West Retail` (state 27) | Intra vs inter-state tax |
| Supplier `DEMO Wholesale Supplies` | Party only — no extra stock purchase |
| Grocery walk-in paid sale + one biscuit **return** | Barcode-style checkout, GP ₹32 after return |
| North A06 + charger paid (UPI) + charger replacement claim | Phone GP ₹1,298; warranty replacement is stock, not a sale |
| West cover with ₹100 partial | Profit ignores payments (₹149) |
| `DEMO shop rent` / `DEMO electricity` | Expenses |

## POS barcode scan

On **Sales → New sale**:

1. Click **Walk-in** (or pick a customer).
2. Focus **Scan barcode or SKU**, type `DEMO-BC-GRC-RICE-001` or `GRC-RICE-001`, press Enter.
3. Scan the same code again: quantity increases by 1 up to available stock.
4. An unknown code shows **No product matches barcode or SKU**.
5. A third scan of `DEMO-BC-SAM-S24FE-8128` (stock 2) is blocked.

Phones that require a serial will prompt for one serial per unit before you can complete the invoice.

## Excel test file

Use **Products → Import Excel → Download template**. The Sample sheet is documentation; only the Products sheet is imported.

`OpeningStock` / `StockQuantity` sets on-hand stock. `AddStockQuantity` adds a delivery. Blank is not zero. Existing products can be updated; Excel will not reduce stock.

`Documentation/samples/product-import-test.xlsx` has mixed valid/invalid/duplicate rows for a preview check. Do not import it into the live shop database.

## Reset

Seed does not delete anything. `BillingSoftware_Verify` was cleared on 2026-10-29 with `Reset-VerifyShop.ps1` (see `DEMO-RESET-PLAN.md`). Do not run that script against `BillingSoftwareDb`. Soft-deleting a sample product hides it from new sales; existing invoices stay for history.

## Known limits

- Demo invoices appear in reports of that database. Keep them off the live shop database.
- Returns: one DEMO cable return is seeded; create others from the invoice page so quantities stay consistent.
- Quantity remains a **whole number**. Packed kg/litre items are billed as packets/pieces until a decimal-qty migration is approved.
- Serial numbers are warranty/lookup fields, not serial-level inventory.
- Company logo is not replaced by a demo file.
- `Demo:Enabled` defaults to false in `appsettings.json`.
