-- Reset transactional/catalogue data on BillingSoftware_Verify only.
-- Preserves users, roles, company, taxes, brands, units, sequences, migrations.
-- No TRUNCATE, no DROP, no identity reseed.
-- Refuses to run if the connected database is not BillingSoftware_Verify.

SET XACT_ABORT ON;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF DB_NAME() <> N'BillingSoftware_Verify'
BEGIN
    DECLARE @wrongDb sysname = DB_NAME();
    DECLARE @msg nvarchar(400);
    SET @msg = N'Refusing to reset database [' + @wrongDb + N']. This script only runs on BillingSoftware_Verify.';
    RAISERROR(@msg, 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.WarrantyClaimEvents', N'U') IS NOT NULL
    DELETE FROM dbo.WarrantyClaimEvents;
IF OBJECT_ID(N'dbo.WarrantyClaims', N'U') IS NOT NULL
    DELETE FROM dbo.WarrantyClaims;
IF OBJECT_ID(N'dbo.Warranties', N'U') IS NOT NULL
    DELETE FROM dbo.Warranties;

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
