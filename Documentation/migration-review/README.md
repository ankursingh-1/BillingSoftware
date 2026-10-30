# Migration review (not executed)

Re-read 2026-10-30 with `SELECT DB_NAME(), COUNT(*), MigrationId` only. **Nothing was applied.**

| Database | Applied count | Last applied | Pending | `ProductImportBatches` |
|---|---|---|---|---|
| `BillingSoftwareDb` | 22 | `20261029063526_AddProductImportBatches` | none | present |
| `BillingSoftware_Verify` | 21 | `20261028114940_AddRetailWarrantyAndBarcode` | 1 (`AddProductImportBatches`) | missing |

The 2026-10-29 table below is historical. Do **not** run the live FINAL script against `BillingSoftwareDb`; that database is already at the repository head.

Verify-only procedure (backup, guards, not executed): `VERIFY-ONLY-AddProductImportBatches.md` and `BillingSoftware_Verify-ONLY-AddProductImportBatches.sql`.

## Historical snapshot (2026-10-29, superseded)

Generated 2026-10-29 from EF Core 8 (`dotnet ef migrations script --idempotent`).
Connection used for generation was **BillingSoftware_Verify** only. These scripts were **not applied**.

| Database | Applied count | Last applied | Pending |
|---|---|---|---|
| `BillingSoftwareDb` | 15 | `20260728070428_AddAppSettings` | 7 (GST snapshots through import batches) |
| `BillingSoftware_Verify` | 21 | `20261028114940_AddRetailWarrantyAndBarcode` | 1 (`AddProductImportBatches`) |

Repository latest: `20261029063526_AddProductImportBatches`.

## Scripts

**Run this one for live (after COPY_ONLY backup):**  
`BillingSoftwareDb-FINAL-AddAppSettings-to-AddProductImportBatches.sql`  
Guards `DB_NAME() = BillingSoftwareDb`, prints duplicate/stock pre-checks, then the EF idempotent body. SSMS steps: `SSMS-BillingSoftwareDb-FINAL.md`. **Not executed.**

Also in this folder (review copies, not the SSMS entry point):

1. `BillingSoftwareDb-pending-from-AddAppSettings-to-AddProductImportBatches.sql` — original EF output (no database-name guard).
2. `BillingSoftware_Verify-pending-AddProductImportBatches.sql` — Verify-only last migration.

Do not run any of these against `BillingSoftware_Verify` as a “live” apply, and do not run the FINAL file until a backup exists.

## Sequence (live)

1. `20261028080238_AddGstSnapshotsAndDocumentSequences`
2. `20261028080322_AddSalePayments`
3. `20261028080339_AddReturnDocuments`
4. `20261028080346_AddAuditLogAndStaffRole`
5. `20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings`
6. `20261028114940_AddRetailWarrantyAndBarcode`
7. `20261029063526_AddProductImportBatches`

## Destructive / data-impact review (Up path)

No `DROP TABLE`, `DROP COLUMN`, `TRUNCATE`, or `DELETE FROM` in the **Up** scripts.

| Operation | Effect | Shop-data handling |
|---|---|---|
| `THROW` if duplicate invoice/purchase numbers or `Products.Stock < 0` | Stops GST migration before unique indexes / check constraint | **Must be checked on a restored copy first.** Not re-queried on live shop rows in this pass. |
| `UPDATE Sales SET Subtotal = TotalAmount` and same for Purchases | Copies legacy totals into the new Subtotal column. Tax flags stay “not calculated”. | Does not change `TotalAmount`. Does not invent GST. |
| `CK_Products_Stock_NonNegative` | Rejects negative stock | Fails if any product stock is negative. |
| Unique `InvoiceNumber` / `PurchaseNumber` | Enforces uniqueness | Fails if duplicates exist. |
| `PaymentTracked` default `0` | Existing invoices are **Not recorded**, not Unpaid. Outstanding is treated as 0 until a new tracked sale. | Historical cash/UPI is **not** backfilled into `Payments`. Collections reports ignore untracked sales. |
| `SaleItems.CostPrice` nullable, no backfill | Old lines stay unknown cost | Gross profit uses complete invoices only. Optional preview: `Documentation/gross-profit-cost-backfill-preview.sql` (preview only). |
| `ShopType` default `GeneralRetail` | Company row gets a default | Counter defaults only. |
| Unique barcode (filtered) | New column, all NULL | Multiple NULLs allowed. |
| Staff role `IF NOT EXISTS` insert | Adds Staff if missing | Idempotent. Down() deletes Staff only with no users. |
| Import-batch tables | Empty new tables + unique FileHash / ReceiptKey | No existing row rewrite. FKs `ON DELETE NO ACTION`. |

Foreign keys on new tables use restrict / no action. They do not cascade-delete products or sales.

## Backup and restore (pending unless you have later evidence)

Never restore over `BillingSoftwareDb` or `BillingSoftware_Verify`. Use a new name such as `BillingSoftware_MigrateTest`.

### 1. Fresh full backup of BillingSoftwareDb

```sql
-- Review/run in SSMS against master. Change the path.
BACKUP DATABASE [BillingSoftwareDb]
TO DISK = N'C:\Backup\BillingSoftwareDb_full_YYYYMMDD.bak'
WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;
```

`COPY_ONLY` avoids breaking the existing backup chain.

### 2. Verify backup metadata

```sql
RESTORE HEADERONLY
FROM DISK = N'C:\Backup\BillingSoftwareDb_full_YYYYMMDD.bak';

RESTORE VERIFYONLY
FROM DISK = N'C:\Backup\BillingSoftwareDb_full_YYYYMMDD.bak'
WITH CHECKSUM;
```

Confirm `DatabaseName = BillingSoftwareDb`, `BackupType = 1` (full), and `IsCopyOnly = 1`.

**This pass:** HEADERONLY / VERIFYONLY / restore were **not** re-run. Treat them as pending. Duplicate invoice/purchase numbers, NULL document numbers, and negative stock were counted read-only on live 2026-10-30: **all 0**.

### 3. Restore into a disposable database

```sql
RESTORE DATABASE [BillingSoftware_MigrateTest]
FROM DISK = N'C:\Backup\BillingSoftwareDb_full_YYYYMMDD.bak'
WITH MOVE N'BillingSoftwareDb' TO N'C:\Data\BillingSoftware_MigrateTest.mdf',
     MOVE N'BillingSoftwareDb_log' TO N'C:\Data\BillingSoftware_MigrateTest_log.ldf',
     REPLACE;  -- only valid because the target name is new, not BillingSoftwareDb
```

Confirm logical file names with `RESTORE FILELISTONLY` first. Do not use `REPLACE` against `BillingSoftwareDb`.

### 4. Confirm the copy is readable

```sql
SELECT DB_NAME() AS DbName;  -- must be BillingSoftware_MigrateTest
SELECT COUNT(*) AS Applied FROM __EFMigrationsHistory;  -- expect 15
```

### 5. Prerequisites before an approved live migration

On the **copy**, not live:

1. Duplicate invoice numbers / purchase numbers / negative stock (the THROW queries in the GST script).
2. Apply the live pending script to the copy.
3. Start the API against the copy only (`Start-TestApi.ps1 -Database BillingSoftware_MigrateTest`).
4. Smoke login, products, a sale, reports.
5. Only then request approval to take another COPY_ONLY backup of live and apply the same script.

## Status

- Script generated: **yes**
- Script reviewed: **yes** (this file)
- Script executed: **no**
- Live `BillingSoftwareDb` modified: **no**
- Backup/restore test this pass: **pending**
