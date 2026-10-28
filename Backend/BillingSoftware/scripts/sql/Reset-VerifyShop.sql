-- Reset transactional/catalogue data on BillingSoftware_Verify only.
-- Preserves users, roles, company, taxes, brands, units, sequences, migrations.
-- No TRUNCATE, no DROP, no identity reseed.
-- Refuses to run if the connected database is not BillingSoftware_Verify.

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'BillingSoftware_Verify'
BEGIN
    RAISERROR(
        'Refusing to reset database [%s]. This script only runs on BillingSoftware_Verify.',
        16, 1, DB_NAME());
    RETURN;
END;

BEGIN TRANSACTION;

DELETE FROM dbo.SalesReturnItems;
DELETE FROM dbo.SalesReturns;
DELETE FROM dbo.PurchaseReturnItems;
DELETE FROM dbo.PurchaseReturns;
DELETE FROM dbo.Payments;
DELETE FROM dbo.SaleItems;
DELETE FROM dbo.Sales;
DELETE FROM dbo.PurchaseItems;
DELETE FROM dbo.Purchases;
DELETE FROM dbo.StockLedgers;
DELETE FROM dbo.Expenses;
DELETE FROM dbo.Products;
DELETE FROM dbo.Customers;
DELETE FROM dbo.Suppliers;
DELETE FROM dbo.Categories;
DELETE FROM dbo.AuditLogs;

COMMIT TRANSACTION;

PRINT 'BillingSoftware_Verify shop data cleared. Users, company, taxes, brands and units were kept.';
