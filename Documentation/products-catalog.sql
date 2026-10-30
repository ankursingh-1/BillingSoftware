-- Product catalogue SQL for BillingSoftware (SQL Server / EF Core 8).
--
-- Default mode is LIST (read-only). This file does not migrate, truncate,
-- drop, or write to BillingSoftwareDb. SEED refuses unless DB_NAME() is
-- exactly BillingSoftware_Verify (binary collation).
--
--   LIST     List active products with lookups. SELECT only.
--   GENERATE Print INSERT statements from the current catalogue (no writes).
--            Do not execute the printed INSERT/UPDATE text.
--   SEED     Idempotent demo products matching DemoDataService (Verify only).
--
-- sqlcmd flags: do not combine -y 0 with -h or -W, and do not combine -W
-- with -Y. Those pairs are rejected by sqlcmd.
--
-- LIST (read-only), Windows auth, QUOTED_IDENTIFIER ON, no truncation:
--   sqlcmd -S localhost -d BillingSoftware_Verify -E -I -y 0 -i Documentation\products-catalog.sql
--
-- GENERATE: change @Mode to N'GENERATE', then the same command. The result
-- is INSERT text only; it is not executed by this script.
--
-- SEED: change @Mode to N'SEED' only after explicit approval. Never point
-- -d at BillingSoftwareDb.

SET XACT_ABORT ON;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Mode nvarchar(20) = N'LIST'; -- LIST | GENERATE | SEED

IF @Mode NOT IN (N'LIST', N'GENERATE', N'SEED')
BEGIN
    RAISERROR(N'Unknown @Mode. Use LIST, GENERATE, or SEED.', 16, 1);
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- LIST — read-only catalogue (SELECT only, then RETURN).
-- ---------------------------------------------------------------------------
IF @Mode = N'LIST'
BEGIN
    SELECT
        p.Id,
        p.Name,
        p.SKU,
        p.Barcode,
        p.PurchasePrice,
        p.SellingPrice,
        p.Stock,
        p.HsnCode,
        p.WarrantyDurationValue,
        p.WarrantyDurationUnit,
        p.RequiresSerialNumber,
        c.Name AS Category,
        b.Name AS Brand,
        u.Name AS Unit,
        t.Name AS Tax,
        t.Percentage AS TaxPercentage,
        p.CreatedOn,
        p.IsDeleted
    FROM dbo.Products p
    LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
    LEFT JOIN dbo.Brands     b ON b.Id = p.BrandId
    LEFT JOIN dbo.Units      u ON u.Id = p.UnitId
    LEFT JOIN dbo.Taxes      t ON t.Id = p.TaxId
    WHERE p.IsDeleted = 0
    ORDER BY p.Name, p.Id;

    SELECT
        COUNT(*) AS ActiveProducts,
        SUM(CASE WHEN p.Stock = 0 THEN 1 ELSE 0 END) AS ZeroStock,
        SUM(CASE WHEN p.Barcode IS NOT NULL THEN 1 ELSE 0 END) AS WithBarcode
    FROM dbo.Products p
    WHERE p.IsDeleted = 0;

    RETURN;
END;

-- ---------------------------------------------------------------------------
-- GENERATE — emit INSERT SQL. This block is SELECT only.
-- Do not execute the printed statements from this review pass.
-- Each product is a separate sqlcmd/SSMS batch (GO) so @ProductId is not
-- redeclared in the same batch. Stock starts at 0; opening qty goes through
-- StockLedgers (type 1 = OpeningStock).
-- ---------------------------------------------------------------------------
IF @Mode = N'GENERATE'
BEGIN
    SELECT
        N'-- Generated from [' + DB_NAME() + N'] product Id ' + CONVERT(nvarchar(20), p.Id) + NCHAR(13) + NCHAR(10)
        + N'SET XACT_ABORT ON;' + NCHAR(13) + NCHAR(10)
        + N'SET QUOTED_IDENTIFIER ON;' + NCHAR(13) + NCHAR(10)
        + N'IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N' + QUOTENAME(p.SKU, '''') + N' AND IsDeleted = 0)' + NCHAR(13) + NCHAR(10)
        + CASE WHEN p.Barcode IS NULL THEN N''
          ELSE N'AND NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Barcode = N' + QUOTENAME(p.Barcode, '''') + N' AND IsDeleted = 0)' + NCHAR(13) + NCHAR(10)
          END
        + N'BEGIN' + NCHAR(13) + NCHAR(10)
        + N'    DECLARE @ProductId int;' + NCHAR(13) + NCHAR(10)
        + N'    INSERT INTO dbo.Products (' + NCHAR(13) + NCHAR(10)
        + N'        Name, SKU, Barcode, PurchasePrice, SellingPrice, Stock, HsnCode,' + NCHAR(13) + NCHAR(10)
        + N'        WarrantyDurationValue, WarrantyDurationUnit, RequiresSerialNumber,' + NCHAR(13) + NCHAR(10)
        + N'        CategoryId, BrandId, UnitId, TaxId,' + NCHAR(13) + NCHAR(10)
        + N'        IsDeleted, CreatedOn, CreatedBy)' + NCHAR(13) + NCHAR(10)
        + N'    VALUES (' + NCHAR(13) + NCHAR(10)
        + N'        N' + QUOTENAME(p.Name, '''') + N',' + NCHAR(13) + NCHAR(10)
        + N'        N' + QUOTENAME(p.SKU, '''') + N',' + NCHAR(13) + NCHAR(10)
        + CASE WHEN p.Barcode IS NULL THEN N'        NULL,' ELSE N'        N' + QUOTENAME(p.Barcode, '''') + N',' END + NCHAR(13) + NCHAR(10)
        + N'        ' + CONVERT(nvarchar(40), p.PurchasePrice) + N',' + NCHAR(13) + NCHAR(10)
        + N'        ' + CONVERT(nvarchar(40), p.SellingPrice) + N',' + NCHAR(13) + NCHAR(10)
        + N'        0,' + NCHAR(13) + NCHAR(10)
        + CASE WHEN p.HsnCode IS NULL THEN N'        NULL,' ELSE N'        N' + QUOTENAME(p.HsnCode, '''') + N',' END + NCHAR(13) + NCHAR(10)
        + CASE WHEN p.WarrantyDurationValue IS NULL THEN N'        NULL,' ELSE N'        ' + CONVERT(nvarchar(20), p.WarrantyDurationValue) + N',' END + NCHAR(13) + NCHAR(10)
        + CASE WHEN p.WarrantyDurationUnit IS NULL THEN N'        NULL,' ELSE N'        N' + QUOTENAME(p.WarrantyDurationUnit, '''') + N',' END + NCHAR(13) + NCHAR(10)
        + N'        ' + CASE WHEN p.RequiresSerialNumber = 1 THEN N'1' ELSE N'0' END + N',' + NCHAR(13) + NCHAR(10)
        + CASE WHEN c.Name IS NULL THEN N'        NULL,' ELSE N'        (SELECT TOP (1) Id FROM dbo.Categories WHERE Name = N' + QUOTENAME(c.Name, '''') + N' AND IsDeleted = 0),' END + NCHAR(13) + NCHAR(10)
        + CASE WHEN b.Name IS NULL THEN N'        NULL,' ELSE N'        (SELECT TOP (1) Id FROM dbo.Brands WHERE Name = N' + QUOTENAME(b.Name, '''') + N' AND IsDeleted = 0),' END + NCHAR(13) + NCHAR(10)
        + CASE WHEN u.Name IS NULL THEN N'        NULL,' ELSE N'        (SELECT TOP (1) Id FROM dbo.Units WHERE Name = N' + QUOTENAME(u.Name, '''') + N' AND IsDeleted = 0),' END + NCHAR(13) + NCHAR(10)
        + CASE WHEN t.Name IS NULL THEN N'        NULL,' ELSE N'        (SELECT TOP (1) Id FROM dbo.Taxes WHERE Name = N' + QUOTENAME(t.Name, '''') + N' AND IsDeleted = 0 AND IsActive = 1),' END + NCHAR(13) + NCHAR(10)
        + N'        0, SYSUTCDATETIME(), N''sql-generate'');' + NCHAR(13) + NCHAR(10)
        + N'    SET @ProductId = SCOPE_IDENTITY();' + NCHAR(13) + NCHAR(10)
        + CASE WHEN p.Stock > 0 THEN
            N'    UPDATE dbo.Products SET Stock = Stock + ' + CONVERT(nvarchar(20), p.Stock)
            + N' WHERE Id = @ProductId AND IsDeleted = 0 AND Stock + ' + CONVERT(nvarchar(20), p.Stock) + N' >= 0;' + NCHAR(13) + NCHAR(10)
            + N'    IF @@ROWCOUNT <> 1' + NCHAR(13) + NCHAR(10)
            + N'        RAISERROR(N''Stock update failed for SKU ' + REPLACE(p.SKU, '''', '''''') + N'.'', 16, 1);' + NCHAR(13) + NCHAR(10)
            + N'    INSERT INTO dbo.StockLedgers (ProductId, TransactionType, Quantity, PreviousStock, CurrentStock, ReferenceNo, Remarks, IsDeleted, CreatedOn, CreatedBy)' + NCHAR(13) + NCHAR(10)
            + N'    VALUES (@ProductId, 1, ' + CONVERT(nvarchar(20), p.Stock) + N', 0, ' + CONVERT(nvarchar(20), p.Stock)
            + N', N' + QUOTENAME(p.SKU, '''') + N', N''Opening stock'', 0, SYSUTCDATETIME(), N''sql-generate'');' + NCHAR(13) + NCHAR(10)
          ELSE N'' END
        + N'END;' + NCHAR(13) + NCHAR(10)
        + N'GO' AS InsertSql
    FROM dbo.Products p
    LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
    LEFT JOIN dbo.Brands     b ON b.Id = p.BrandId
    LEFT JOIN dbo.Units      u ON u.Id = p.UnitId
    LEFT JOIN dbo.Taxes      t ON t.Id = p.TaxId
    WHERE p.IsDeleted = 0
    ORDER BY p.Name, p.Id;

    RETURN;
END;

-- ---------------------------------------------------------------------------
-- SEED — demo catalogue (DemoDataService grocery + Samsung samples).
-- Refuses unless DB_NAME() is exactly BillingSoftware_Verify.
-- Idempotent by active SKU. Does not reduce stock. Does not delete.
-- ---------------------------------------------------------------------------
IF @Mode <> N'SEED'
BEGIN
    RAISERROR(N'SEED block reached with @Mode other than SEED.', 16, 1);
    RETURN;
END;

IF DB_NAME() COLLATE Latin1_General_BIN <> N'BillingSoftware_Verify' COLLATE Latin1_General_BIN
BEGIN
    DECLARE @wrongDb sysname = DB_NAME();
    DECLARE @msg nvarchar(400);
    SET @msg = N'Refusing SEED on [' + @wrongDb + N']. This script only inserts on BillingSoftware_Verify.';
    RAISERROR(@msg, 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now datetime2 = SYSUTCDATETIME();
    DECLARE @By nvarchar(100) = N'sql-demo-seed';

    -- Unique indexes on lookup Name have no IsDeleted filter. Do not INSERT
    -- when a deleted row already occupies the name.
    IF NOT EXISTS (SELECT 1 FROM dbo.Taxes WHERE Name = N'GST 5%')
        INSERT INTO dbo.Taxes (Name, Percentage, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'GST 5%', 5.00, N'Demo tax', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Taxes WHERE Name = N'GST 18%')
        INSERT INTO dbo.Taxes (Name, Percentage, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'GST 18%', 18.00, N'Demo tax', 1, 0, @Now, @By);

    IF NOT EXISTS (SELECT 1 FROM dbo.Units WHERE Name = N'Piece')
        INSERT INTO dbo.Units (Name, ShortName, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Piece', N'Pcs', N'Demo unit', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Units WHERE Name = N'Packet')
        INSERT INTO dbo.Units (Name, ShortName, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Packet', N'Pkt', N'Demo unit', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Units WHERE Name = N'Kilogram')
        INSERT INTO dbo.Units (Name, ShortName, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Kilogram', N'Kg', N'Demo unit', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Units WHERE Name = N'Litre')
        INSERT INTO dbo.Units (Name, ShortName, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Litre', N'L', N'Demo unit', 1, 0, @Now, @By);

    IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Samsung')
        INSERT INTO dbo.Brands (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Samsung', N'Demo brand', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Aashirvaad')
        INSERT INTO dbo.Brands (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Aashirvaad', N'Demo brand', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Tata')
        INSERT INTO dbo.Brands (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Tata', N'Demo brand', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Fortune')
        INSERT INTO dbo.Brands (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Fortune', N'Demo brand', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Parle')
        INSERT INTO dbo.Brands (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Parle', N'Demo brand', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Britannia')
        INSERT INTO dbo.Brands (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Britannia', N'Demo brand', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Amul')
        INSERT INTO dbo.Brands (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Amul', N'Demo brand', 1, 0, @Now, @By);

    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'Grocery')
        INSERT INTO dbo.Categories (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Grocery', N'Demo category', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'Packaged Food')
        INSERT INTO dbo.Categories (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Packaged Food', N'Demo category', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'Beverages')
        INSERT INTO dbo.Categories (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Beverages', N'Demo category', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'Rice & Pulses')
        INSERT INTO dbo.Categories (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Rice & Pulses', N'Demo category', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'Mobile Phones')
        INSERT INTO dbo.Categories (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Mobile Phones', N'Demo category', 1, 0, @Now, @By);
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'Mobile Accessories')
        INSERT INTO dbo.Categories (Name, Description, IsActive, IsDeleted, CreatedOn, CreatedBy)
        VALUES (N'Mobile Accessories', N'Demo category', 1, 0, @Now, @By);

    DECLARE @Gst5 int = (SELECT TOP (1) Id FROM dbo.Taxes WHERE Name = N'GST 5%' AND IsDeleted = 0 AND IsActive = 1);
    DECLARE @Gst18 int = (SELECT TOP (1) Id FROM dbo.Taxes WHERE Name = N'GST 18%' AND IsDeleted = 0 AND IsActive = 1);
    DECLARE @Piece int = (SELECT TOP (1) Id FROM dbo.Units WHERE Name = N'Piece' AND IsDeleted = 0);
    DECLARE @Packet int = (SELECT TOP (1) Id FROM dbo.Units WHERE Name = N'Packet' AND IsDeleted = 0);
    DECLARE @Litre int = (SELECT TOP (1) Id FROM dbo.Units WHERE Name = N'Litre' AND IsDeleted = 0);
    DECLARE @Samsung int = (SELECT TOP (1) Id FROM dbo.Brands WHERE Name = N'Samsung' AND IsDeleted = 0);
    DECLARE @Aashirvaad int = (SELECT TOP (1) Id FROM dbo.Brands WHERE Name = N'Aashirvaad' AND IsDeleted = 0);
    DECLARE @Tata int = (SELECT TOP (1) Id FROM dbo.Brands WHERE Name = N'Tata' AND IsDeleted = 0);
    DECLARE @Fortune int = (SELECT TOP (1) Id FROM dbo.Brands WHERE Name = N'Fortune' AND IsDeleted = 0);
    DECLARE @Parle int = (SELECT TOP (1) Id FROM dbo.Brands WHERE Name = N'Parle' AND IsDeleted = 0);
    DECLARE @Britannia int = (SELECT TOP (1) Id FROM dbo.Brands WHERE Name = N'Britannia' AND IsDeleted = 0);
    DECLARE @Amul int = (SELECT TOP (1) Id FROM dbo.Brands WHERE Name = N'Amul' AND IsDeleted = 0);
    DECLARE @Grocery int = (SELECT TOP (1) Id FROM dbo.Categories WHERE Name = N'Grocery' AND IsDeleted = 0);
    DECLARE @Packaged int = (SELECT TOP (1) Id FROM dbo.Categories WHERE Name = N'Packaged Food' AND IsDeleted = 0);
    DECLARE @Beverages int = (SELECT TOP (1) Id FROM dbo.Categories WHERE Name = N'Beverages' AND IsDeleted = 0);
    DECLARE @RicePulses int = (SELECT TOP (1) Id FROM dbo.Categories WHERE Name = N'Rice & Pulses' AND IsDeleted = 0);
    DECLARE @Mobiles int = (SELECT TOP (1) Id FROM dbo.Categories WHERE Name = N'Mobile Phones' AND IsDeleted = 0);
    DECLARE @Accessories int = (SELECT TOP (1) Id FROM dbo.Categories WHERE Name = N'Mobile Accessories' AND IsDeleted = 0);

    IF @Gst5 IS NULL OR @Gst18 IS NULL
        OR @Piece IS NULL OR @Packet IS NULL OR @Litre IS NULL
        OR @Samsung IS NULL OR @Aashirvaad IS NULL OR @Tata IS NULL
        OR @Fortune IS NULL OR @Parle IS NULL OR @Britannia IS NULL OR @Amul IS NULL
        OR @Grocery IS NULL OR @Packaged IS NULL OR @Beverages IS NULL
        OR @RicePulses IS NULL OR @Mobiles IS NULL OR @Accessories IS NULL
    BEGIN
        RAISERROR(N'SEED aborted: a required category, brand, unit, or active tax was not found.', 16, 1);
    END;

    DECLARE @Seed TABLE (
        Name nvarchar(200) NOT NULL,
        SKU nvarchar(200) NOT NULL PRIMARY KEY,
        Barcode nvarchar(80) NULL,
        PurchasePrice decimal(18,2) NOT NULL,
        SellingPrice decimal(18,2) NOT NULL,
        OpeningStock int NOT NULL,
        HsnCode nvarchar(8) NULL,
        WarrantyValue int NULL,
        WarrantyUnit nvarchar(20) NULL,
        RequiresSerial bit NOT NULL,
        CategoryId int NULL,
        BrandId int NULL,
        UnitId int NULL,
        TaxId int NULL
    );

    INSERT INTO @Seed (
        Name, SKU, Barcode, PurchasePrice, SellingPrice, OpeningStock, HsnCode,
        WarrantyValue, WarrantyUnit, RequiresSerial, CategoryId, BrandId, UnitId, TaxId)
    VALUES
        (N'Rice 1 kg (demo)', N'GRC-RICE-001', N'DEMO-BC-GRC-RICE-001', 55, 70, 100, N'1006', NULL, NULL, 0, @RicePulses, @Tata, @Packet, @Gst5),
        (N'Toor Dal 1 kg (demo)', N'GRC-DAL-001', N'DEMO-BC-GRC-DAL-001', 100, 125, 60, N'0713', NULL, NULL, 0, @RicePulses, @Tata, @Packet, @Gst5),
        (N'Sugar 1 kg (demo)', N'GRC-SUGAR-001', N'DEMO-BC-GRC-SUGAR-001', 38, 45, 80, N'1701', NULL, NULL, 0, @Grocery, @Tata, @Packet, @Gst5),
        (N'Atta 5 kg (demo)', N'GRC-ATTA-005', N'DEMO-BC-GRC-ATTA-005', 180, 220, 30, N'1101', NULL, NULL, 0, @Grocery, @Aashirvaad, @Packet, @Gst5),
        (N'Sunflower Oil 1 L (demo)', N'GRC-OIL-001', N'DEMO-BC-GRC-OIL-001', 115, 140, 50, N'1512', NULL, NULL, 0, @Grocery, @Fortune, @Litre, @Gst5),
        (N'Salt 1 kg (demo)', N'GRC-SALT-001', N'DEMO-BC-GRC-SALT-001', 18, 22, 100, N'2501', NULL, NULL, 0, @Grocery, @Tata, @Packet, @Gst5),
        (N'Parle-G Biscuit Pack (demo)', N'GRC-BISC-001', N'DEMO-BC-GRC-BISC-001', 8, 10, 100, N'1905', NULL, NULL, 0, @Packaged, @Parle, @Packet, @Gst5),
        (N'Britannia Bread (demo)', N'GRC-BREAD-001', N'DEMO-BC-GRC-BREAD-001', 30, 35, 40, N'1905', NULL, NULL, 0, @Packaged, @Britannia, @Packet, @Gst5),
        (N'Amul Butter (demo)', N'GRC-BUTTER-001', N'DEMO-BC-GRC-BUTTER-001', 50, 60, 40, N'0405', NULL, NULL, 0, @Packaged, @Amul, @Packet, @Gst5),
        (N'Mineral Water 1 L (demo)', N'GRC-WATER-001', N'DEMO-BC-GRC-WATER-001', 12, 20, 80, N'2201', NULL, NULL, 0, @Beverages, @Amul, @Litre, @Gst5),
        (N'Samsung Galaxy A06 4GB/64GB (demo)', N'SAM-A06-464', N'DEMO-BC-SAM-A06-464', 7000, 7999, 5, N'8517', 1, N'Years', 1, @Mobiles, @Samsung, @Piece, @Gst18),
        (N'Samsung Galaxy A16 5G 8GB/128GB (demo)', N'SAM-A16-8128', N'DEMO-BC-SAM-A16-8128', 16000, 18999, 5, N'8517', 1, N'Years', 1, @Mobiles, @Samsung, @Piece, @Gst18),
        (N'Samsung Galaxy A26 5G 8GB/128GB (demo)', N'SAM-A26-8128', N'DEMO-BC-SAM-A26-8128', 22000, 24999, 3, N'8517', 1, N'Years', 1, @Mobiles, @Samsung, @Piece, @Gst18),
        (N'Samsung Galaxy M35 5G 8GB/128GB (demo)', N'SAM-M35-8128', N'DEMO-BC-SAM-M35-8128', 17000, 19999, 4, N'8517', 1, N'Years', 1, @Mobiles, @Samsung, @Piece, @Gst18),
        (N'Samsung Galaxy S24 FE 8GB/128GB (demo)', N'SAM-S24FE-8128', N'DEMO-BC-SAM-S24FE-8128', 35000, 39999, 2, N'8517', 1, N'Years', 1, @Mobiles, @Samsung, @Piece, @Gst18),
        (N'Samsung USB-C Charger (demo)', N'SAM-CHARGER-001', N'DEMO-BC-SAM-CHARGER-001', 500, 799, 10, N'8504', 6, N'Months', 0, @Accessories, @Samsung, @Piece, @Gst18),
        (N'USB-C Cable (demo)', N'SAM-CABLE-001', N'DEMO-BC-SAM-CABLE-001', 100, 199, 20, N'8544', NULL, NULL, 0, @Accessories, @Samsung, @Piece, @Gst18),
        (N'Samsung Phone Cover (demo)', N'SAM-COVER-001', N'DEMO-BC-SAM-COVER-001', 150, 299, 15, N'3926', NULL, NULL, 0, @Accessories, @Samsung, @Piece, @Gst18),
        (N'Tempered Glass (demo)', N'SAM-GLASS-001', N'DEMO-BC-SAM-GLASS-001', 50, 149, 20, N'7007', NULL, NULL, 0, @Accessories, @Samsung, @Piece, @Gst18);

    IF EXISTS (
        SELECT Barcode FROM @Seed WHERE Barcode IS NOT NULL
        GROUP BY Barcode HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR(N'SEED aborted: duplicate barcode in the seed table.', 16, 1);
    END;

    DECLARE @Name nvarchar(200), @SKU nvarchar(200), @Barcode nvarchar(80);
    DECLARE @Purchase decimal(18,2), @Selling decimal(18,2);
    DECLARE @Opening int, @Hsn nvarchar(8), @WVal int, @WUnit nvarchar(20);
    DECLARE @Serial bit, @CatId int, @BrandId int, @UnitId int, @TaxId int;
    DECLARE @ProductId int, @Inserted int = 0, @Skipped int = 0;
    DECLARE @Err nvarchar(400);

    DECLARE seed_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, SKU, Barcode, PurchasePrice, SellingPrice, OpeningStock, HsnCode,
               WarrantyValue, WarrantyUnit, RequiresSerial, CategoryId, BrandId, UnitId, TaxId
        FROM @Seed;

    OPEN seed_cursor;
    FETCH NEXT FROM seed_cursor INTO
        @Name, @SKU, @Barcode, @Purchase, @Selling, @Opening, @Hsn,
        @WVal, @WUnit, @Serial, @CatId, @BrandId, @UnitId, @TaxId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @ProductId = NULL;
        SELECT @ProductId = Id
        FROM dbo.Products
        WHERE SKU = @SKU AND IsDeleted = 0;

        IF @ProductId IS NOT NULL
        BEGIN
            IF @Barcode IS NOT NULL
               AND EXISTS (
                    SELECT 1
                    FROM dbo.Products
                    WHERE Barcode = @Barcode
                      AND IsDeleted = 0
                      AND Id <> @ProductId)
            BEGIN
                SET @Err = N'SEED aborted: barcode already belongs to another product (SKU ' + @SKU + N').';
                RAISERROR(@Err, 16, 1);
            END;

            UPDATE dbo.Products
            SET Barcode = CASE WHEN Barcode IS NULL THEN @Barcode ELSE Barcode END,
                WarrantyDurationValue = CASE WHEN WarrantyDurationValue IS NULL THEN @WVal ELSE WarrantyDurationValue END,
                WarrantyDurationUnit = CASE WHEN WarrantyDurationUnit IS NULL THEN @WUnit ELSE WarrantyDurationUnit END,
                RequiresSerialNumber = CASE WHEN @Serial = 1 THEN 1 ELSE RequiresSerialNumber END,
                modifieson = @Now,
                ModifiedBy = @By
            WHERE Id = @ProductId
              AND (
                    (Barcode IS NULL AND @Barcode IS NOT NULL)
                 OR (WarrantyDurationValue IS NULL AND @WVal IS NOT NULL)
                 OR (WarrantyDurationUnit IS NULL AND @WUnit IS NOT NULL)
                 OR (@Serial = 1 AND RequiresSerialNumber = 0)
              );

            SET @Skipped = @Skipped + 1;
        END
        ELSE
        BEGIN
            IF @Barcode IS NOT NULL
               AND EXISTS (
                    SELECT 1
                    FROM dbo.Products
                    WHERE Barcode = @Barcode AND IsDeleted = 0)
            BEGIN
                SET @Err = N'SEED aborted: barcode already exists (SKU ' + @SKU + N').';
                RAISERROR(@Err, 16, 1);
            END;

            INSERT INTO dbo.Products (
                Name, SKU, Barcode, PurchasePrice, SellingPrice, Stock, HsnCode,
                WarrantyDurationValue, WarrantyDurationUnit, RequiresSerialNumber,
                CategoryId, BrandId, UnitId, TaxId,
                IsDeleted, CreatedOn, CreatedBy)
            VALUES (
                @Name, @SKU, @Barcode, @Purchase, @Selling, 0, @Hsn,
                @WVal, @WUnit, @Serial,
                @CatId, @BrandId, @UnitId, @TaxId,
                0, @Now, @By);

            SET @ProductId = SCOPE_IDENTITY();

            IF @Opening > 0
            BEGIN
                UPDATE dbo.Products
                SET Stock = Stock + @Opening
                WHERE Id = @ProductId AND IsDeleted = 0 AND Stock + @Opening >= 0;

                IF @@ROWCOUNT <> 1
                BEGIN
                    SET @Err = N'SEED aborted: stock update failed for SKU ' + @SKU + N'.';
                    RAISERROR(@Err, 16, 1);
                END;

                INSERT INTO dbo.StockLedgers (
                    ProductId, TransactionType, Quantity, PreviousStock, CurrentStock,
                    ReferenceNo, Remarks, IsDeleted, CreatedOn, CreatedBy)
                VALUES (
                    @ProductId, 1, @Opening, 0, @Opening,
                    @SKU, N'Opening stock', 0, @Now, @By);
            END;

            SET @Inserted = @Inserted + 1;
        END;

        FETCH NEXT FROM seed_cursor INTO
            @Name, @SKU, @Barcode, @Purchase, @Selling, @Opening, @Hsn,
            @WVal, @WUnit, @Serial, @CatId, @BrandId, @UnitId, @TaxId;
    END;

    CLOSE seed_cursor;
    DEALLOCATE seed_cursor;

    COMMIT TRANSACTION;

    SELECT @Inserted AS ProductsInserted, @Skipped AS ProductsAlreadyPresent;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY
        CLOSE seed_cursor;
        DEALLOCATE seed_cursor;
    END TRY
    BEGIN CATCH
    END CATCH;

    THROW;
END CATCH;
