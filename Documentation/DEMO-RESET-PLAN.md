# Demo / test database reset plan

**Status: EXECUTED 2026-10-29 on `BillingSoftware_Verify` only.**

Copy-only backup taken first. `BillingSoftwareDb` was not reset.

## Databases identified

| Database | How it is used | Environment |
|---|---|---|
| `BillingSoftwareDb` | Visual Studio / User Secrets `DefaultConnection` on `DESKTOP-O9II3LE` (Windows auth). **Live/dev shop data.** 15 migrations. | Development User Secrets |
| `BillingSoftware_Verify` | `Start-TestApi.ps1` default (`http://localhost:7190`). Test copy with extra payments/returns tables (20 migrations). | Isolated verification |
| Tracked `appsettings.json` | `ConnectionStrings:DefaultConnection` is **empty** (secrets live in User Secrets / env vars). | All |

No other `BillingSoftware*` databases exist on this SQL Server.

**Proposed reset target: `BillingSoftware_Verify` only.**

**Will not touch:** `BillingSoftwareDb`.

## Backup (already taken, copy-only)

| Field | Value |
|---|---|
| Database | `BillingSoftware_Verify` |
| Type | Full, **COPY_ONLY** (does not break any backup chain) |
| When | 2026-10-28 16:40:11 |
| File | SQL default backup folder: `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\BillingSoftware_Verify_pre-demo-reset_20261028.bak` |
| Header check | `RESTORE HEADERONLY` confirmed `DatabaseName=BillingSoftware_Verify`, `IsCopyOnly=1` |

Restore (only if you later need to undo a reset):

```sql
RESTORE DATABASE [BillingSoftware_Verify]
FROM DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\BillingSoftware_Verify_pre-demo-reset_20261028.bak'
WITH REPLACE;
```

## What would be cleared (Verify only)

Transactional and catalogue **shop data**, in FK-safe `DELETE` order (no `TRUNCATE`, no `DROP`):

| Category | Tables | Current rows (Verify) |
|---|---|---|
| Warranty claims | `WarrantyClaimEvents`, `WarrantyClaims` | present after `AddRetailWarrantyAndBarcode` |
| Warranties | `Warranties` | present after `AddRetailWarrantyAndBarcode` |
| Sales returns | `SalesReturnItems`, `SalesReturns` | 6 / 5 |
| Purchase returns | `PurchaseReturnItems`, `PurchaseReturns` | 4 / 3 |
| Payments | `Payments` | 21 |
| Sales | `SaleItems`, `Sales` | 62 / 59 |
| Purchases | `PurchaseItems`, `Purchases` | 25 / 23 |
| Stock | `StockLedgers` | 132 |
| Expenses | `Expenses` | 3 |
| Products | `Products` | 40 |
| Parties | `Customers`, `Suppliers` | 41 / 16 |
| Test categories | `Categories` | 8 |
| Audit noise | `AuditLogs` | 106 |

## What would be preserved

| Category | Tables |
|---|---|
| Logins | `Users`, `Roles` (Admin and Staff on Verify stay) |
| Company settings / logo path | `Companies` |
| Tax / brand / unit lists | `Taxes`, `Brands`, `Units` |
| Invoice number counters | `DocumentSequences` (next invoice continues; not reset) |
| Schema | `__EFMigrationsHistory` |
| Legacy unused | `AppSettings` |

Identity (`Users.Id`, etc.) is **not** reseeded.

## Procedure (after you approve)

From `Backend/BillingSoftware/scripts`:

```powershell
.\Reset-VerifyShop.ps1 -IConfirmReset 'RESET BillingSoftware_Verify'
```

The script:

1. Refuses any database other than `BillingSoftware_Verify`.
2. Requires the exact confirmation string above.
3. Runs `scripts/sql/Reset-VerifyShop.sql` inside a transaction (`XACT_ABORT`).
4. Does not start seeding; you seed afterwards from Settings with `Demo:Enabled=true` on the test API.

## After reset

1. Start the test API: `.\Start-TestApi.ps1 -Database BillingSoftware_Verify -Port 7190`
2. Set `Demo__Enabled=true` for that process only (see `DEMO-GUIDE.md`).
3. Sign in as the existing Verify Admin (`verify.admin@test.local`).
4. Settings → **Seed demo records**.
5. Optionally rename the company in Settings to **Demo Electronics Store**. The seed does **not** overwrite GSTIN/state.

## Execution (2026-10-29)

Ran from `Backend/BillingSoftware/scripts`:

```powershell
.\Reset-VerifyShop.ps1 -IConfirmReset 'RESET BillingSoftware_Verify'
```

`sqlcmd` needed `-x` (so `%s` is not treated as a variable) and `SET QUOTED_IDENTIFIER ON` because of filtered indexes. After reset, Admin seed created the grocery/Samsung sample catalogue. Do **not** run this script against `BillingSoftwareDb`.
