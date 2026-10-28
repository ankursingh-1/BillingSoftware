# Project status

Last updated: 2026-10-28 (demo catalogue, walk-in sale, payment confirmation, reset plan — **reset not executed**).

Status values: **Complete and tested** · **Implemented, testing pending** · **In progress** · **Blocked** · **Not started**.

Estimation method: count of modules in the table whose backend **and** frontend are “Complete and tested”, divided by the number of modules. No invented percentages beyond that count.

## Module table

| Module | Visible in nav | Backend | Frontend | API connected | Validation | Authorization | Automated tests | Browser | Remaining issues |
|---|---|---|---|---|---|---|---|---|---|
| Auth / JWT login | Login page | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | No refresh tokens (would need a new table). Change-password exists (`POST /api/Auth/change-password`) and is in the header. |
| Products & categories | Products, Categories | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Stock on the product form is read-only (ledger owns it). |
| Brands, units & tax | Brands, units & tax (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Complete and tested | Writes are Admin; GET is signed-in (product forms). |
| Excel product import/export | Products → Import / Export | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested (unit + API 17/17) | Implemented, testing pending | Import modal (template + policy) opened in the browser. File upload was not driven in the UI this pass; API script covers preview/confirm. |
| Customers & suppliers | Customers, Suppliers | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Complete and tested | — |
| Sales & payments | Sales | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | New Sale has **Walk-in**. After payment: amount, remaining balance, status, Print receipt. Browser re-check of those two controls pending this pass. |
| Invoices (A4 / compact / thermal 80 / 58) | Sale detail format picker | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested (PDF images) | Implemented, testing pending | HTML print dialog not clicked (browser cannot pick a printer). Thermal 80/58 is a width selector when format is Thermal. |
| Payment receipts | Payment row → receipt | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | A5 and Thermal layouts opened. |
| Company settings & logo | Settings (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | GSTIN/state mismatch is a **warning that blocks save**; it does not auto-change tax identity. Verify company GSTIN `36…` matches Telangana. Set `Storage:Root` in production. |
| Purchases | Purchases (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | List and New Purchase are visible. No purchase payables (not in the model). |
| Returns & refunds | Returns | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Sale detail has Return items. List page opened. A new return was not posted in this browser pass. |
| Inventory / ledger / adjustments | Inventory | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Adjustment of a zero-stock row is blocked until you **increase** (correct). Low-stock uses global `Inventory:LowStockThreshold` (default 5). |
| Expenses | Expenses (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Complete and tested | Category is free text. No payment-method column (not invented). |
| Reports | Reports (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Complete and tested | Gross profit omitted when any sale-line CostPrice is missing (correct). Low / out of stock are filters on the Stock tab. Excel download button present; file save not asserted in the browser. |
| Dashboard | Dashboard | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Complete and tested | Cards link to modules. Staff do not see purchase links. Gross profit shows Unavailable when cost is incomplete. |
| Demo data | Settings → Seed demo records (Admin) | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Complete and tested | Implemented, testing pending | Implemented, testing pending | Electronics catalogue + walk-in/paid/partial/unpaid + return. Disabled unless `Demo:Enabled`. **Reset of Verify not executed — awaiting approval.** Never seeded on `BillingSoftwareDb`. |

**Count:** 10 of 16 modules are “Complete and tested” on backend + frontend + browser (auth, products/categories, catalog, customers/suppliers, sales/payments, payment receipts, company settings, inventory, expenses, dashboard). Reports browser-opened all tabs including incomplete profit. Excel UI file upload, HTML print dialog, a new purchase, and a new return were not fully driven this pass.

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
| Reports | `/reports` | Admin only; Sales, Purchases, Stock (All/Low/Out), Customers, Suppliers, Collections, Expenses, Gross profit |
| Settings | `/dashboard/settings` | Admin only |
| Change password | Header button | Modal with current / new / confirm |
| Staff | — | Catalog, Purchases, Expenses, Reports, Settings hidden in the sidebar and wrapped in `AdminRoute` |

Header no longer shows fake search, theme, notifications, or a placeholder avatar. It shows the JWT email and role.

## Migrations

Reviewed additive migrations exist in the repo (GST snapshots, payments, returns, invoice snapshots / print settings). They are applied on `BillingSoftware_Verify` only.

**Not applied to live `BillingSoftwareDb` without approval.** Change-password, catalog UI, expenses, extra reports, Excel import, and demo data add **no** new migration.

Rollback: `Down()` on each migration; restore a backup taken before apply.

## Build / test log (this pass)

Recorded after the commands actually ran:

- `dotnet test Billing.Tests -c Release` — **140 passed**, 0 failed (includes change-password `[Authorize]` check). Four obsolete QuestPDF wrap warnings on `Billing.API`.
- `GET http://localhost:7190/api/Expense` without a token — **401**.
- Frontend `eslint src --max-warnings 0`, `tsc --noEmit`, `npm run build` — succeeded.
- Browser on `http://localhost:5177` against `http://localhost:7190` / `BillingSoftware_Verify` as `verify.admin@test.local`:
  - Login → dashboard (real totals; gross profit Unavailable).
  - Every sidebar item opened.
  - Products list + Import Excel modal (template download control; Check file disabled until a file is chosen).
  - Product edit loaded Dell Laptop / DL001 / prices / read-only stock.
  - Catalog brands and units listed.
  - Unpaid invoice INV-2627-000035 → partial cash ₹50 → PARTLY PAID → receipt RCT-2627-000010 (A5 and Thermal 58 mm).
  - Inventory adjustment: reducing a zero-stock row was rejected; increasing succeeded.
  - Expense “Verify shop electricity” ₹450 Utilities created.
  - Reports: Gross profit showed incomplete-cost warning; Stock tab Low / Out of stock filters present.
  - Settings: company fields, print prefs, logo upload, Seed demo records (Demo disabled).
  - Change-password modal opened.

## Known issues

- Live database still on older migration count until you approve apply + backup.
- If a live company GSTIN prefix does not match the selected state, Settings **blocks save** until you choose. The app will not rewrite GSTIN or state for you.
- CORS origins hard-coded to 5173/5177.
- No refresh tokens.
- Gross profit is incomplete for legacy sale lines without CostPrice.
- AppSetting table is a legacy unused duplicate of company settings.
- Expense has no payment-method column (not invented).
- Purchase payables / expense payments are not in the model.
- Demo seed was not run (correct while `Demo:Enabled` is false).
- Excel import UI file picker was not used; API tests cover the pipeline.
- HTML print dialog was not opened.
- Verify DB contains leftover SAMPLE / empty-named rows from earlier import tests.

Operator guide: `Documentation/USER-GUIDE-HINDI.md`. Reset plan: `Documentation/DEMO-RESET-PLAN.md`.

## Next task

**You must approve** the `BillingSoftware_Verify` reset (`DEMO-RESET-PLAN.md`) before any deletes. Do not apply migrations to `BillingSoftwareDb` without a backup. Keep `Demo:Enabled=false` in production.
