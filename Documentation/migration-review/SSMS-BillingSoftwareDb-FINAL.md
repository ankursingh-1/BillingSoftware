# SSMS: final BillingSoftwareDb migration

**Do not run this until a COPY_ONLY backup exists.** The agent did not execute this script.

Script: `Documentation/migration-review/BillingSoftwareDb-FINAL-AddAppSettings-to-AddProductImportBatches.sql`

Target: **BillingSoftwareDb only**. Wrong database names abort and `SET NOEXEC ON` skips the remaining batches.

## 1. Backup (run against `master`)

Change the path.

```sql
BACKUP DATABASE [BillingSoftwareDb]
TO DISK = N'C:\Backup\BillingSoftwareDb_full_YYYYMMDD.bak'
WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;
```

```sql
RESTORE HEADERONLY
FROM DISK = N'C:\Backup\BillingSoftwareDb_full_YYYYMMDD.bak';

RESTORE VERIFYONLY
FROM DISK = N'C:\Backup\BillingSoftwareDb_full_YYYYMMDD.bak'
WITH CHECKSUM;
```

Confirm `DatabaseName = BillingSoftwareDb`, `BackupType = 1`, `IsCopyOnly = 1`.

## 2. Confirm the SSMS connection

New query window. Toolbar database = `BillingSoftwareDb`.

```sql
SELECT DB_NAME() AS DbName;
-- Expect: BillingSoftwareDb

SELECT MigrationId
FROM __EFMigrationsHistory
ORDER BY MigrationId;
-- Last row must be 20260728070428_AddAppSettings (15 rows).
```

Query → SQLCMD Mode is optional. Keep QUOTED_IDENTIFIER ON (SSMS default).

## 3. Execute the script

1. File → Open the FINAL `.sql` file.
2. Confirm the connected database is still `BillingSoftwareDb`.
3. Execute (F5) the **entire** file.
4. First result set should show all pre-check counts = 0.
5. Messages should include seven `__EFMigrationsHistory` inserts (or skip already-applied batches if you re-run).

sqlcmd equivalent (not run here):

```text
sqlcmd -S localhost -d BillingSoftwareDb -E -I -b -i Documentation\migration-review\BillingSoftwareDb-FINAL-AddAppSettings-to-AddProductImportBatches.sql
```

`-I` is required for filtered indexes. `-b` stops on error.

Each of the seven migrations **COMMITs on its own**. If a later step fails, earlier ones stay applied. Re-run the same file; it is idempotent.

## 4. Post-migration verification (read-only)

```sql
SELECT DB_NAME() AS DbName;
-- Must still be BillingSoftwareDb

SELECT MigrationId
FROM __EFMigrationsHistory
ORDER BY MigrationId;
```

Expect **22** rows. Last: `20261029063526_AddProductImportBatches`. The seven new ids:

```
20261028080238_AddGstSnapshotsAndDocumentSequences
20261028080322_AddSalePayments
20261028080339_AddReturnDocuments
20261028080346_AddAuditLogAndStaffRole
20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings
20261028114940_AddRetailWarrantyAndBarcode
20261029063526_AddProductImportBatches
```

```sql
-- New tables exist
SELECT name FROM sys.tables
WHERE name IN (
    'DocumentSequences','Payments','PurchaseReturns','PurchaseReturnItems',
    'SalesReturns','SalesReturnItems','AuditLogs','Warranties',
    'WarrantyClaims','WarrantyClaimEvents',
    'ProductImportBatches','ProductImportReceipts')
ORDER BY name;
-- Expect 12 names.

-- Constraints
SELECT name FROM sys.check_constraints
WHERE name IN (
    'CK_Products_Stock_NonNegative',
    'CK_Payments_Amount_Positive',
    'CK_Companies_ThermalWidth');

-- Subtotal copy (new column should match TotalAmount on legacy rows)
SELECT
    SUM(CASE WHEN Subtotal = TotalAmount THEN 1 ELSE 0 END) AS SubtotalMatchesTotal,
    COUNT(*) AS SalesRows
FROM Sales;

SELECT
    SUM(CASE WHEN Subtotal = TotalAmount THEN 1 ELSE 0 END) AS SubtotalMatchesTotal,
    COUNT(*) AS PurchaseRows
FROM Purchases;

-- PaymentTracked default: existing rows are 0 (Not recorded)
SELECT PaymentTracked, COUNT(*) AS Cnt
FROM Sales
GROUP BY PaymentTracked;
-- Expect PaymentTracked = 0 for all pre-migration sales.

SELECT COUNT(*) AS PaymentRows FROM Payments;
-- Expect 0 until new receipts are taken.

-- Company / product defaults
SELECT ShopType, DefaultInvoiceFormat, ThermalPaperWidthMm FROM Companies;
-- ShopType GeneralRetail, format A4, width 80 unless later changed in Settings.

SELECT
    SUM(CASE WHEN RequiresSerialNumber = 1 THEN 1 ELSE 0 END) AS SerialRequired,
    SUM(CASE WHEN Barcode IS NOT NULL THEN 1 ELSE 0 END) AS WithBarcode
FROM Products
WHERE IsDeleted = 0;
-- Both 0 until you set them on products.

SELECT COUNT(*) AS StaffRoles FROM Roles WHERE Name = N'Staff';
-- 1 if Staff was missing; otherwise unchanged.
```

Then start the API against `BillingSoftwareDb` (Visual Studio / production host — do not switch connection strings in source). Smoke: login, products, one sale, reports.

## 5. What this script does not do

- Does not drop tables or columns.
- Does not delete shop rows.
- Does not backfill `Payments` from historical cash (there was no Payments table).
- Does not backfill `SaleItems.CostPrice`.
- Does not seed demo products.
- Does not restore or overwrite the database.
