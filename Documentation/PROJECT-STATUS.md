# Project status

Last updated: 2026-10-30 (read-only migration re-check). **Classification: Testing Complete — Deployment Pending.** Not Production Ready until production `Cors:Origins` / `Storage:Root` / `JwtSettings__Key` are set on the host. On localhost, `BillingSoftwareDb` has **22** migrations (none pending) and `BillingSoftware_Verify` has **21** (only `AddProductImportBatches` pending). That re-check was `SELECT` only. No migration was applied on 2026-10-30. Verify UI **5177**. No demo seed, Git commit, or push.

Status values: **Complete and tested** · **Implemented, testing pending** · **In progress** · **Blocked** · **Not started**.

Estimation method: count of modules in the table whose backend **and** frontend are “Complete and tested”, divided by the number of modules. No invented percentages beyond that count.

## Module table

| Module | Visible in nav | Backend | Frontend | API connected | Validation | Authorization | Automated tests | Browser | Remaining issues |
|---|---|---|---|---|---|---|---|---|---|
| Auth / JWT login | Login page | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | No refresh tokens (would need a new table). Change-password exists (`POST /api/Auth/change-password`) and is in the header. |
| Products & categories | Products, Categories | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Stock on the product form is read-only (ledger owns it). |
| Brands, units & tax | Brands, units & tax (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Complete and tested | Writes are Admin; GET is signed-in (product forms). |
| Excel product import/export | Products → Import / Export | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested (planner + apply + prior API script) | Complete and tested | Existing SKU/barcode is a match (update + stock difference), not “duplicate / do not overwrite”. OpeningStock is desired on-hand; confirm reloads live stock. File-row duplicates stay separate. Isolated-browser OS file picker is blocked; this pass attached the workbook via same-origin fetch + DataTransfer into the hidden input (same `handleFile` path). `AddProductImportBatches` not applied. |
| Customers & suppliers | Customers, Suppliers | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Complete and tested | — |
| Sales & payments | Sales | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Invoice detail loads payments from the Payments table. Status is Unpaid / Partial / Paid from PaymentCalculator. Duplicate `ClientRequestId` replays the same invoice. Overpay returns 409. |
| Invoices (A4 / compact / thermal 80 / 58) | Sale detail format picker | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested (PDF images) | Complete and tested | Print button called `window.print()` on invoice and receipt pages. The OS print dialog cannot be completed in this isolated browser. Thermal 80/58 is a width selector when format is Thermal. |
| Payment receipts | Payment row → receipt | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | A5 and Thermal layouts opened. |
| Company settings & logo | Settings (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | GSTIN/state mismatch is a **warning that blocks save**; it does not auto-change tax identity. Verify company GSTIN `36…` matches Telangana. Set `Storage:Root` in production. |
| Purchases | Purchases (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | UI purchase `PUR-2627-000017` (2× salt @ ₹18 + 5% + round ₹0.20 = ₹38.00). Ledger qty 2, 100→102. No purchase payables (not in the model). |
| Returns & refunds | Returns | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | UI return of 1× biscuit on `INV-2627-000043` created `CN-2627-000007`, refund ₹10.50 cash, stock 99→100. |
| Inventory / ledger / adjustments | Inventory | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Adjustment of a zero-stock row is blocked until you **increase** (correct). Low-stock uses global `Inventory:LowStockThreshold` (default 5). |
| Expenses | Expenses (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Complete and tested | Category is free text. No payment-method column (not invented). |
| Reports | Reports (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Gross profit is known/partial when some invoices lack CostPrice; it is not hidden entirely. Excel/PDF use the same calculator. Low / out of stock are filters on the Stock tab. |
| Dashboard | Dashboard | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Summary no longer fails because Warranties is missing or the expiring-warranty call 404s. Widget failures stay metric-specific. Known gross profit this month uses the same calculator as Reports. Outstanding collections uses PaymentCalculator (voids excluded). |
| Demo data | Settings → Seed demo records (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Grocery + Samsung sample catalogue. Disabled unless `Demo:Enabled`. **Verify reset executed 2026-10-29.** Never seeded on `BillingSoftwareDb`. |
| Shop type (Electronics / Grocery / General) | Settings | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Implemented, testing pending | Counter defaults only. One application. |
| Product barcode & warranty defaults | Product form + Excel | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested (import unit tests) | Implemented, testing pending | Changing a default does not rewrite historical warranties. |
| Warranty register, claims, replacement | Warranties | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested (dates, transitions, auth) | Complete and tested | Walk-in cannot register warranty (named customer required). `WRN-2627-000003` registered on POS; claim `CLM-2627-000002` Submitted→UnderInspection→Approved→ReplacementIssued; water stock 79→78. Serials are lookup/warranty fields, not serial-level stock. |
| Grocery POS barcode scan | New sale | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Complete and tested | Repeat scan +1 qty. Quantity stays **integer**. Internal barcodes are `DEMO-BC-{SKU}`. |

**Count:** 18 of 20 modules are “Complete and tested” on backend + frontend + browser. Remaining: Shop type (browser) and Product barcode/warranty defaults on the product form (browser). Native OS Excel file picker is still blocked in this isolated browser; the DataTransfer path was used instead.

Treat the product as **usable for sales, payments, inventory, expenses, reports, and catalog on `BillingSoftware_Verify`**.

## Navigation audit (Admin)

| Item | Route | Result |
|---|---|---|
| Dashboard | `/dashboard` | Loads; cards and recent lists link through |
| Products | `/products` | List, search, pagination, Import/Export, Add |
| Categories | `/categories` | Page and Add Category |
| Brands, units & tax | `/catalog` | Brands / Units / Tax rates CRUD |
| Inventory | `/inventory` | Stock, Movements, Adjustments |
| Purchases | `/purchases` | List + New Purchase (Admin) |
| Sales | `/sales` | List, filters, invoice PDF, detail |
| Returns | `/returns` | Sales returns / Purchase returns |
| Customers | `/customers` | List + Add |
| Suppliers | `/suppliers` | List + Add |
| Expenses | `/expenses` | Admin only |
| Reports | `/reports` | Admin only; plus links to warranty reports |
| Warranties | `/warranties` | Search + claim workflow (signed-in) |
| Settings | `/dashboard/settings` | Admin only; includes shop type |
| Change password | Header button | Modal with current / new / confirm |
| Staff | — | Catalog, Purchases, Expenses, Reports, Settings hidden in the sidebar and wrapped in `AdminRoute` |

Header no longer shows fake search, theme, notifications, or a placeholder avatar. It shows the JWT email and role.

## Gross profit

### Root cause

`SaleItems.CostPrice` (decimal 18,2, nullable) was added in `20261028080238_AddGstSnapshotsAndDocumentSequences` with no backfill. Older invoices and test sales created while catalogue `PurchasePrice` was 0 therefore have `NULL` cost. Reports and Dashboard previously hid **all** gross profit if **any** remaining sold line was null, so the cards read “Unavailable” even when some invoices were fully costed. The stock ledger stores quantities only, so there is no FIFO layer cost; the application’s costing method is last recorded purchase/opening cost snapshotted onto the sale line.

### Calculation rules (Dashboard and Reports share `GrossProfitService` → `GrossProfitCalculator`)

- Revenue = tax-exclusive line amount (`TaxableAmount` / `Total`), net of returned quantity. GST and round-off are not revenue.
- COGS = remaining sold qty × sale-line `CostPrice` snapshot. Missing cost is never treated as zero.
- Known gross profit = complete invoices only (every remaining sold line has a snapshot). Mixed invoices are **Partial**; invoice profit is withheld.
- Fully returned lines need no cost (remaining qty 0).
- Payments, including voids and partials, are not inputs. There is no voided-sale document; a return is the reversal. Warranty replacement is a stock transaction, not a sale.
- Empty period: known profit 0, complete. All incomplete: known profit **null**, not 0.
- There is no line discount field in the model.

### Historical cost data

`BillingSoftware_Verify` was reset 2026-10-29 and reseeded. After this pass’s UI sale/return, dashboard and Reports Gross profit both show **₹1,532.00**, 7 complete invoices, coverage 100%. Opening stock uses `Product.PurchasePrice` as last-cost (ledger is quantity-only). No historical backfill was applied to the discarded test data.

### New sales

`SaleService.CreateAsync` resolves cost (`CostSnapshot.FromCatalogue`, else last purchase line > 0) **before** `AdjustStockAsync`. Zero catalogue cost stays null (unknown, not free).

### Migration status

No new migration. `CostPrice` already exists. Nothing was applied to `BillingSoftwareDb`.

## Migrations

Re-read `__EFMigrationsHistory` (names only) 2026-10-29:

| Database | Applied | Last applied | Pending |
|---|---|---|---|
| `BillingSoftwareDb` | 15 | `20260728070428_AddAppSettings` | 7 |
| `BillingSoftware_Verify` | 21 | `20261028114940_AddRetailWarrantyAndBarcode` | 1 (`AddProductImportBatches`) |

Repository latest: `20261029063526_AddProductImportBatches`.

Review-only idempotent SQL (generated via `dotnet ef migrations script --idempotent`, generation connected to **Verify only**, **not executed**):

- `Documentation/migration-review/BillingSoftwareDb-pending-from-AddAppSettings-to-AddProductImportBatches.sql`
- `Documentation/migration-review/BillingSoftware_Verify-pending-AddProductImportBatches.sql`
- Prerequisites and backup/restore steps: `Documentation/migration-review/README.md`

Up path has **no** `DROP TABLE` / `DROP COLUMN` / `TRUNCATE` / `DELETE`. Data-impact: `UPDATE Sales/Purchases SET Subtotal = TotalAmount`; unique invoice/purchase numbers; non-negative stock check; `PaymentTracked` default 0 (legacy invoices **Not recorded**, not Unpaid); nullable `CostPrice` with no backfill.

**Backup/restore this pass: pending.** Do not apply live until a COPY_ONLY backup, HEADERONLY/VERIFYONLY, and a restore into a **new** database (not over live or Verify) succeed.

Rollback: restore the pre-apply backup. `Down()` exists but restore is the shop-safe path.

## Build / test log (this pass)

Responsive nav + migration review (2026-10-29, later):

- Root cause: `.sidebar` used `min-height: 100vh` without a viewport-capped height; `.sidebar-menu` had `flex: 1` without `min-height: 0` / `overflow-y: auto`. On short windows the 14 Admin items overflowed; items below Customers were clipped. Mobile `position: fixed` had no `top/bottom`, so the overlay could not scroll.
- Fix: viewport-capped flex column (`height/max-height: 100dvh`), menu `min-height: 0; overflow-y: auto`, sticky desktop sidebar, fixed full-height overlay ≤992px, `scrollIntoView` for the active item. No menu items removed. `adminOnly` filter unchanged.
- Viewport checks **actually run** on `http://localhost:5177/reports` (Verify admin session): **1920×1080** (all 14 items, Settings in view); **1366×768** (menu scrollbar, Customers + Suppliers visible, Settings reachable after scroll, no horizontal overflow); **1024×768** (Reports active, Settings visible, page content usable); **768×1024** overlay open (Customers through Settings + Logout); **390×844** overlay (same, `overflow-y: auto`, no horizontal overflow). Toggle: Reports nav click closed the overlay. Staff menu not re-logged this pass (code: `adminOnly` still hides Catalog/Purchases/Expenses/Reports/Settings).
- Product catalogue SQL static review: default `@Mode` LIST (SELECT + RETURN). GENERATE is SELECT-only. SEED binary `DB_NAME()` guard before writes; `@ProductId` NULLed; barcode checks; FK abort; stock via ledger + `@@ROWCOUNT`; TRY/CATCH rollback. Existing-SKU `UPDATE` now skipped when nothing to fill. LIST/GENERATE/SEED **not executed**. Remaining risks: lookup name drift (`GST 5%` vs renamed taxes), no unique SKU index, soft-deleted SKU reuse, GENERATE batches not one transaction.
- `dotnet build BillingSoftware.sln -c Release` — **0 warnings, 0 errors** (verified).
- `dotnet test BillingSoftware.sln -c Release` — **238 passed**, 0 failed, 0 skipped (verified).
- Frontend `npx tsc --noEmit`, `npx eslint src --max-warnings 0`, `npm run build` — succeeded (verified). Bundle warning: `index-*.js` ~966 kB / 268 kB gzip (>500 kB).
- npm audit (no upgrades): **3 high**, **2 moderate**, 0 critical (same family as prior: brace-expansion / follow-redirects / zod / react-router).
- `BillingSoftwareDb` **not written** (history names only). No migration applied. No demo seed. No Git commit/push.

Verify UI workflows on `BillingSoftware_Verify` only (2026-10-29, later; 7190 API / 5177 UI; `DB_NAME()` confirmed):

- Excel Import UI: Cover was 25; Inventory Adjust `ADJ-2627-000010` 25→14. Preview of OpeningStock 20 showed **Will apply 1 / Unchanged 18 / +6**. Confirm: stock **20**, ledger qty 6 Adjustment. Reimport: **Will apply 0 / Unchanged 19**, Import disabled, stock stayed 20. Native OS file picker still cannot be driven; workbook was injected via same-origin fetch + DataTransfer into `input[type=file]` (same React `handleFile` path). Temporary `Frontend/public/cover-opening-20.xlsx` was removed after the check.
- Purchase UI: `PUR-2627-000017` / id 30, DEMO Wholesale, 2× `GRC-SALT-001` @ ₹18, tax ₹1.80, round ₹0.20, **total ₹38.00**. Ledger 100→102, `TransactionType` 2, reference `PUR-2627-000017`.
- POS: Walk-in cannot register warranty. Named customer DEMO North Retail. Barcode Enter `DEMO-BC-GRC-WATER-001` then `DEMO-BC-GRC-BISC-001`. Invoice `INV-2627-000043` Unpaid ₹32; water 80→79, biscuit 100→99. Partial cash ₹15 (`RCT-2627-000019`) → remaining ₹17 (`RCT-2627-000020`) → Paid. Warranty `WRN-2627-000003` serial `VERIFY-WATER-001`.
- Return: biscuit qty 1, `CN-2627-000007`, refund ₹10.50 cash, biscuit 99→100.
- Warranty claim UI: `CLM-2627-000002` Submitted → UnderInspection → Approved → ReplacementIssued (`GRC-WATER-001` SN `VERIFY-WATER-RPL-001`, stock deducted). Water 79→78, ledger `TransactionType` 11, reference `CLM-2627-000002`.
- Password: dedicated staff account changed in the header modal, old password 401, new password login 200 on UI, then restored. Unauthenticated `/sales` redirected to `/login`. Staff `/reports` and `/dashboard/settings` show “You do not have access to this page.” Admin reports load.
- Printing: invoice and receipt Print buttons invoked `window.print()` (count 1 each). OS print dialog cannot be finished here. UI PDF blobs: Compact 107,069; A4 127,120; Thermal 58 65,081; Thermal 80 65,513; receipt A5 84,666; receipt Thermal 50,112 (all `application/pdf`).
- Reports vs SQL (`BillingSoftware_Verify`): Dashboard today sales **₹11,198.00** = seven invoices; today purchases **₹1,928.00** = PUR-016 ₹1,890 + PUR-017 ₹38; products 19; customers 3; suppliers 1; low stock 2 (`SAM-S24FE-8128` 2, `SAM-M35-8128` 4); outstanding collections **₹253.00** = INV-039 ₹353 − ₹100 paid. Reports Sales total ₹11,198.00; Purchases ₹1,928.00; Collections ₹10,945.00 (9 non-voided payments); Stock 19 rows (cover 20, salt 102, water 78, biscuit 100); Gross profit **₹1,532.00** / 7 complete / 100% (INV-043 net sales ₹20 after biscuit return, profit ₹8). Dashboard GP matches Reports.
- `dotnet build BillingSoftware.sln -c Release` — **0 warnings, 0 errors**.
- `dotnet test BillingSoftware.sln -c Release` — **238 passed**, 0 failed, 0 skipped.
- Frontend `npx tsc --noEmit`, `npx eslint src --max-warnings 0`, `npm run build` — succeeded. Bundle warning: `index-*.js` ~965 kB / 268 kB gzip (>500 kB).
- `AddProductImportBatches` **not applied**. `BillingSoftwareDb` **not migrated, not reset, not written**. No GitHub push.

Production-readiness audit (2026-10-29, later):

- Confirmed SQL identities: `BillingSoftware_Verify` (21 migrations) and `BillingSoftwareDb` (15 migrations). Port 7190 process connected to **BillingSoftware_Verify**. Visual Studio 7179 was not required for this pass.
- `dotnet build BillingSoftware.sln -c Release` — succeeded, **0 warnings, 0 errors** after removing obsolete QuestPDF `WrapAnywhere` (layout engine wraps automatically since 2024.3).
- `dotnet test BillingSoftware.sln -c Release` — **238 passed**, 0 failed, 0 skipped.
- Frontend `npx tsc --noEmit`, `npx eslint src --max-warnings 0`, `npm run build` — succeeded. Bundle warning: `index-*.js` ~965 kB / 268 kB gzip (>500 kB).
- Isolated API on 7190: unauthenticated dashboard **401**; bad JWT **401**; admin login 200; staff reports POST **403**; staff dashboard 200.
- Created Verify-only sale `INV-2627-000042` (Walk-in, 1× `GRC-RICE-001`): Unpaid → Partial ₹37 → Paid ₹74. Duplicate client id replayed the same sale. Overpay 409. Stock 98→97 with `StockLedgers` sale qty 1. PDFs A4/Compact/Thermal 80/58 and payment receipt all HTTP 200 with PDF bytes.
- Dashboard vs Reports after that sale: known GP **₹1,524.00** both; coverage 100%; 6 complete invoices. Dashboard **total sales ₹11,166** is tax-inclusive invoice totals; Reports **net sales ₹9,497** is tax-exclusive after returns (expected, not a mismatch of GP).
- Browser `http://localhost:5177` (already signed in as Verify admin): Dashboard cards matched the API; Products; Sales list; Reports Gross profit ₹1,524.00 / 6 complete / 100%; Warranties page; Settings (Creative, GSTIN `36…`, logo replace/remove present, Demo seed button present); Inventory stock/adjust; New Sale barcode field accepted `DEMO-BC-GRC-RICE-001` Enter → qty 1, second scan → qty **2**. Invoice was **not** submitted from the UI (API sale already covered payments).
- Excel UI file picker still cannot be attached via CDP. HTML print dialog not clicked. Password was not changed. A new purchase/return/claim was not posted from the browser (existing replacement claim `CLM-2627-000001` / `ReplacementIssued` read via API).
- `AddProductImportBatches` **not applied**. `BillingSoftwareDb` **not migrated and not reset**.
- NuGet `--vulnerable`: none remaining after pinning test-only `System.Net.Http` 4.3.4 / `System.Text.RegularExpressions` 4.3.1.
- npm audit after axios 1.21.0 / react-router-dom 6.30.6 / vite 8.2.4: remaining **3 high** (transitive `brace-expansion`, `follow-redirects`, `zod`) and **2 moderate** (react-router family). No React Router 7 upgrade.

Smart Excel import (2026-10-29):

- The remaining preview conflict was **not** an identity/duplicate bug. `C:\Users\ASUS\Downloads\products.xlsx` row 14 Samsung Phone Cover (`SAM-COVER-001` / `DEMO-BC-SAM-COVER-001` / id 1056) has **OpeningStock 10** while Verify on-hand was **20**. Planner correctly blocked a silent reduction. Messages now say “Target stock is lower than current stock” with row, SKU, and quantities.
- Copy of that workbook with OpeningStock **20**, after an authorized adjustment back to 14: preview valid=1, unchanged=18, conflict=0, **+6**. Confirm: stock **20**. Reimport: 19 unchanged, stock stayed 20.
- `dotnet test BillingSoftware.sln -c Release` — **236 passed**, 0 failed.
- Frontend `npx tsc --noEmit`, `npx eslint src --max-warnings 0`, `npm run build` — succeeded.
- Verify API on **7190 / `BillingSoftware_Verify`**. Isolated browser cannot attach .xlsx (file-input CDP blocked); Products page showed cover stock **20** after import.
- `AddProductImportBatches` **not applied**. `BillingSoftwareDb` not touched.

Recorded after the commands actually ran (2026-10-29 gross-profit pass):

- `dotnet build BillingSoftware.sln -c Release` — succeeded (existing QuestPDF obsolete warnings only).
- `dotnet test BillingSoftware.sln -c Release` — **183 passed**, 0 failed (gross-profit calculator, cost snapshot order, Excel labels, prior payment/warranty/invoice tests).
- Frontend `npx tsc --noEmit`, `npx eslint src --max-warnings 0`, `npm run build` — succeeded.
- `Start-TestApi.ps1` rebuilt against `BillingSoftware_Verify` on port 7190. `BillingSoftwareDb` not migrated and not reset. No historical `UPDATE` of CostPrice.
- Browser on `http://localhost:5177` against `http://localhost:7190` as `verify.admin@test.local`:
  - Dashboard: **Known gross profit (this month) ₹2,279.97** after the verification sale, with the incomplete-cost warning. Products/today’s sales still loaded. Not “Unavailable”.
  - Reports → Gross profit: net sales, known GP, complete/incomplete counts, coverage %, invoice Cost/Profit/Cost data. Historical rows show **Cost unavailable**.
  - October date filter matched dashboard (36 incomplete invoices, same known GP before the extra sale).
  - Excel (`/Reports/profit/excel`) contains Known gross profit, Cost unavailable, and the limitation text.
  - New sale `INV-2627-000036` stored `CostPrice` 50.00 and appears Complete (₹100 − ₹50 = ₹50).

Verify reset + grocery/Samsung seed (later 2026-10-29):

- `Reset-VerifyShop.ps1 -IConfirmReset 'RESET BillingSoftware_Verify'` — succeeded after enabling `QUOTED_IDENTIFIER` (filtered indexes) and sqlcmd `-x`. Target `DB_NAME()` was `BillingSoftware_Verify`. Copy-only backup `BillingSoftware_Verify_pre-demo-reset_20261028.bak` header-checked.
- Users (`verify.admin@test.local`), company **Creative**, 21 migrations, taxes/brands/units kept. Shop rows cleared.
- Seed via `POST /api/Demo/seed` with `Demo__Enabled=true` on port 7190 only, then API restarted **without** that flag.
- `dotnet test -c Release` — **183 passed**. Frontend `tsc` / `eslint src --max-warnings 0` / `npm run build` — succeeded.
- Browser 5177: dashboard GP ₹1,479.00 complete; 19 products; POS scan rice×2, USB-C cable, invalid barcode toast, S24 FE oversell blocked at stock 2; Reports GP ₹1,479.00 / 100% coverage; Settings company still Creative.

## Known issues

- Localhost `BillingSoftwareDb` is at 22 migrations as of the 2026-10-30 read. Do not run the older “7 pending” live script against it. `BillingSoftware_Verify` still needs `AddProductImportBatches` after an approved backup. Procedure: `Documentation/migration-review/VERIFY-ONLY-AddProductImportBatches.md`. Not applied.
- If a live company GSTIN prefix does not match the selected state, Settings **blocks save** until you choose. The app will not rewrite GSTIN or state for you.
- CORS origins are configured (`Cors:Origins`); Production fails fast if the list is empty. Set `Cors__Origins__0` to the real frontend URL before hosting.
- No refresh tokens (would need a new table). Login rate limit is 10/IP/minute.
- `GET /api/Products` still returns the full catalogue (search is paged). Fine for current shop size.
- Live `BillingSoftwareDb` last COPY_ONLY backup in msdb: 2026-10-28 13:27. Verify COPY_ONLY backup HEADERONLY-checked (`BillingSoftware_Verify_pre-demo-reset_20261028.bak`). **Neither restore was re-executed this pass.**
- After the Verify reset, sample invoices are fully costed. Opening stock is last-cost catalogue price, not FIFO layers.
- AppSetting table is a legacy unused duplicate of company settings.
- Expense has no payment-method column (not invented).
- Purchase payables / expense payments are not in the model.
- Demo seed is disabled again (`Demo:Enabled` not set on the running 7190 API). Never seed `BillingSoftwareDb`.
- Native OS Excel file picker still cannot be driven from this isolated browser; the DataTransfer path was used. Apply `AddProductImportBatches` on Verify when you want full file-hash replay in SQL.
- HTML Print invokes `window.print()`; the OS printer dialog cannot be completed in this isolated browser.
- Verify shop data from earlier import tests was cleared by the 2026-10-29 reset.
- Sale quantity and stock remain **integers**. Decimal kg/litre is not in the ledger; packed units are billed as pieces/packets.
- Product serials are unique on warranty registrations. They are not serial-level inventory.
- No SMS / WhatsApp / email warranty reminders (in-app list only).
- A POS cart with a serial-required phone was not submitted from the browser (serial prompt). Seeded Samsung sale already has a snapshot.

Operator guide: `Documentation/USER-GUIDE-HINDI.md`. Reset plan: `Documentation/DEMO-RESET-PLAN.md`.

## Next task

**Requires approval (Verify only):** `AddProductImportBatches` on `BillingSoftware_Verify` after the COPY_ONLY backup in `Documentation/migration-review/VERIFY-ONLY-AddProductImportBatches.md`. Do not run that script on `BillingSoftwareDb` — the 2026-10-30 read already shows 22 migrations and the import tables present. Set production `Cors:Origins`, `Storage:Root`, and `JwtSettings__Key`. Keep `Demo:Enabled=false`. Do not reset either database. Do not run `products-catalog.sql` SEED on live.
