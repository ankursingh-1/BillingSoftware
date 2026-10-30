# Verify-only: AddProductImportBatches

Status: **prepared, not executed.** Do not run this against `BillingSoftwareDb`.

Read on 2026-10-30 (localhost, Windows auth, `SELECT` only):

| Database | Applied | Last migration | `ProductImportBatches` |
|---|---|---|---|
| `BillingSoftware_Verify` | 21 | `20261028114940_AddRetailWarrantyAndBarcode` | missing |
| `BillingSoftwareDb` | 22 | `20261029063526_AddProductImportBatches` | present |

## What the migration does

`20261029063526_AddProductImportBatches` creates two empty tables:

- `ProductImportBatches` (unique `FileHash`)
- `ProductImportReceipts` (unique `ReceiptKey`, foreign keys to the batch and to `Products`, `ON DELETE NO ACTION`)

It does not `UPDATE`, `DELETE`, `TRUNCATE`, or drop columns. `Down()` drops those two new tables only. Shop invoices, stock, and users are untouched. The guarded script below repeats that `Up()` in one batch so a failed guard cannot continue into later batches.

The older file `BillingSoftware_Verify-pending-AddProductImportBatches.sql` is the EF idempotent script **without** a database-name guard. Do not run that file. Use the guarded script.

## Procedure (when you approve it)

1. In SSMS, connect to `BillingSoftware_Verify` and run only this read:

```sql
SELECT DB_NAME() AS DbName, COUNT(*) AS Applied
FROM dbo.__EFMigrationsHistory;

SELECT MigrationId
FROM dbo.__EFMigrationsHistory
WHERE MigrationId = N'20261029063526_AddProductImportBatches';

SELECT CASE WHEN OBJECT_ID(N'dbo.ProductImportBatches', N'U') IS NULL
            THEN N'missing' ELSE N'present' END AS ImportTable;
```

Expect `BillingSoftware_Verify`, `21`, no history row, and `missing`. If the name is `BillingSoftwareDb`, stop.

2. Copy-only backup of **Verify**, not live:

```sql
BACKUP DATABASE [BillingSoftware_Verify]
TO DISK = N'C:\Backup\BillingSoftware_Verify_pre-import-batches_YYYYMMDD.bak'
WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;
```

3. Check the backup. Do not restore it over either shop database.

```sql
RESTORE HEADERONLY
FROM DISK = N'C:\Backup\BillingSoftware_Verify_pre-import-batches_YYYYMMDD.bak';

RESTORE VERIFYONLY
FROM DISK = N'C:\Backup\BillingSoftware_Verify_pre-import-batches_YYYYMMDD.bak'
WITH CHECKSUM;
```

Confirm `DatabaseName = BillingSoftware_Verify`, `BackupType = 1`, `IsCopyOnly = 1`.

4. Optional: restore that backup to a **new** name such as `BillingSoftware_VerifyMigTest`. Confirm logical file names with `RESTORE FILELISTONLY` first. Do not use `REPLACE` against `BillingSoftware_Verify` or `BillingSoftwareDb`.

5. On Verify (or on the new test copy, after editing the guard to that new name), run `BillingSoftware_Verify-ONLY-AddProductImportBatches.sql`. The script throws unless `DB_NAME()` is `BillingSoftware_Verify`, the history count is 21, and the import tables are absent. It then commits the two tables and the history row.

6. Re-run the read in step 1. Expect 22 applied, the history row present, and `present`.

Rollback, if required before any import has been saved: restore the backup from step 2 into a new database and point a test API at it. Do not restore over `BillingSoftwareDb`.
