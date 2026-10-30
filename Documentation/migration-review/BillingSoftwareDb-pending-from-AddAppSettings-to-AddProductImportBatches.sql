-- REVIEW ONLY. Do not execute against BillingSoftwareDb without a fresh backup and explicit approval.
-- Generated 2026-10-29: dotnet ef migrations script 20260728070428_AddAppSettings --idempotent
-- From (exclusive): 20260728070428_AddAppSettings
-- To: 20261029063526_AddProductImportBatches
-- Generation connected to BillingSoftware_Verify only. This file was not applied.
BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN

    IF EXISTS (SELECT 1 FROM Sales GROUP BY InvoiceNumber HAVING COUNT(*) > 1)
        THROW 50001, 'Migration stopped: duplicate invoice numbers exist in Sales. Resolve them, then re-run.', 1;
    IF EXISTS (SELECT 1 FROM Purchases GROUP BY PurchaseNumber HAVING COUNT(*) > 1)
        THROW 50002, 'Migration stopped: duplicate purchase numbers exist in Purchases. Resolve them, then re-run.', 1;
    IF EXISTS (SELECT 1 FROM Products WHERE Stock < 0)
        THROW 50003, 'Migration stopped: products with negative stock exist. Correct them with a stock adjustment, then re-run.', 1;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [Gstin] nvarchar(15) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [State] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [StateCode] nvarchar(2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [CgstTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [ClientRequestId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [CompanyGstin] nvarchar(15) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [CompanyStateCode] nvarchar(2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [CustomerGstin] nvarchar(15) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [IgstTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [IsInterState] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [PlaceOfSupplyStateCode] nvarchar(2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [RoundOff] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [SgstTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [Subtotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [TaxCalculated] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Sales] ADD [TaxTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [SaleItems] ADD [CgstAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [SaleItems] ADD [CostPrice] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [SaleItems] ADD [HsnCode] nvarchar(8) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [SaleItems] ADD [IgstAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [SaleItems] ADD [SgstAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [SaleItems] ADD [TaxAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [SaleItems] ADD [TaxName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [SaleItems] ADD [TaxRate] decimal(5,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [SaleItems] ADD [TaxableAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [CgstTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [ClientRequestId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [IgstTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [IsInterState] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [RoundOff] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [SgstTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [Subtotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [SupplierGstin] nvarchar(15) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [SupplierInvoiceNumber] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [TaxCalculated] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Purchases] ADD [TaxTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [CgstAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [HsnCode] nvarchar(8) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [IgstAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [SgstAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [TaxAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [TaxName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [TaxRate] decimal(5,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [TaxableAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Products] ADD [HsnCode] nvarchar(8) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Customers] ADD [Gstin] nvarchar(15) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Customers] ADD [State] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Customers] ADD [StateCode] nvarchar(2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    ALTER TABLE [Companies] ADD [StateCode] nvarchar(2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    CREATE TABLE [DocumentSequences] (
        [SeriesKey] nvarchar(50) NOT NULL,
        [LastNumber] int NOT NULL,
        CONSTRAINT [PK_DocumentSequences] PRIMARY KEY ([SeriesKey])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Sales_ClientRequestId] ON [Sales] ([ClientRequestId]) WHERE [ClientRequestId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sales_InvoiceNumber] ON [Sales] ([InvoiceNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    CREATE INDEX [IX_Sales_SaleDate] ON [Sales] ([SaleDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Purchases_ClientRequestId] ON [Purchases] ([ClientRequestId]) WHERE [ClientRequestId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    CREATE INDEX [IX_Purchases_PurchaseDate] ON [Purchases] ([PurchaseDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Purchases_PurchaseNumber] ON [Purchases] ([PurchaseNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    EXEC(N'ALTER TABLE [Products] ADD CONSTRAINT [CK_Products_Stock_NonNegative] CHECK ([Stock] >= 0)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    UPDATE Sales SET Subtotal = TotalAmount;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    UPDATE Purchases SET Subtotal = TotalAmount;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080238_AddGstSnapshotsAndDocumentSequences'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028080238_AddGstSnapshotsAndDocumentSequences', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080322_AddSalePayments'
)
BEGIN
    ALTER TABLE [Sales] ADD [PaymentTracked] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080322_AddSalePayments'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [SaleId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [Method] int NOT NULL,
        [Reference] nvarchar(100) NULL,
        [Notes] nvarchar(500) NULL,
        [IsVoided] bit NOT NULL,
        [VoidedOn] datetime2 NULL,
        [VoidedBy] nvarchar(200) NULL,
        [VoidReason] nvarchar(500) NULL,
        [CreatedOn] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [modifieson] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payments_Amount_Positive] CHECK ([Amount] > 0),
        CONSTRAINT [FK_Payments_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080322_AddSalePayments'
)
BEGIN
    CREATE INDEX [IX_Payments_SaleId] ON [Payments] ([SaleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080322_AddSalePayments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028080322_AddSalePayments', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE TABLE [PurchaseReturns] (
        [Id] int NOT NULL IDENTITY,
        [ReturnNumber] nvarchar(50) NOT NULL,
        [PurchaseId] int NOT NULL,
        [ReturnDate] datetime2 NOT NULL,
        [Reason] nvarchar(500) NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [CgstTotal] decimal(18,2) NOT NULL,
        [SgstTotal] decimal(18,2) NOT NULL,
        [IgstTotal] decimal(18,2) NOT NULL,
        [TaxTotal] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [RefundReceived] decimal(18,2) NOT NULL,
        [RefundMethod] int NULL,
        [AdjustmentNote] nvarchar(500) NULL,
        [CreatedOn] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [modifieson] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_PurchaseReturns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseReturns_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE TABLE [SalesReturns] (
        [Id] int NOT NULL IDENTITY,
        [ReturnNumber] nvarchar(50) NOT NULL,
        [SaleId] int NOT NULL,
        [ReturnDate] datetime2 NOT NULL,
        [Reason] nvarchar(500) NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [CgstTotal] decimal(18,2) NOT NULL,
        [SgstTotal] decimal(18,2) NOT NULL,
        [IgstTotal] decimal(18,2) NOT NULL,
        [TaxTotal] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [RefundAmount] decimal(18,2) NOT NULL,
        [RefundMethod] int NULL,
        [Remarks] nvarchar(500) NULL,
        [CreatedOn] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [modifieson] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_SalesReturns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalesReturns_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE TABLE [PurchaseReturnItems] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseReturnId] int NOT NULL,
        [PurchaseItemId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TaxableAmount] decimal(18,2) NOT NULL,
        [TaxRate] decimal(5,2) NOT NULL,
        [CgstAmount] decimal(18,2) NOT NULL,
        [SgstAmount] decimal(18,2) NOT NULL,
        [IgstAmount] decimal(18,2) NOT NULL,
        [TaxAmount] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PurchaseReturnItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseReturnItems_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_PurchaseReturnItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReturnItems_PurchaseItems_PurchaseItemId] FOREIGN KEY ([PurchaseItemId]) REFERENCES [PurchaseItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReturnItems_PurchaseReturns_PurchaseReturnId] FOREIGN KEY ([PurchaseReturnId]) REFERENCES [PurchaseReturns] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE TABLE [SalesReturnItems] (
        [Id] int NOT NULL IDENTITY,
        [SalesReturnId] int NOT NULL,
        [SaleItemId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TaxableAmount] decimal(18,2) NOT NULL,
        [TaxRate] decimal(5,2) NOT NULL,
        [CgstAmount] decimal(18,2) NOT NULL,
        [SgstAmount] decimal(18,2) NOT NULL,
        [IgstAmount] decimal(18,2) NOT NULL,
        [TaxAmount] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_SalesReturnItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesReturnItems_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_SalesReturnItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesReturnItems_SaleItems_SaleItemId] FOREIGN KEY ([SaleItemId]) REFERENCES [SaleItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesReturnItems_SalesReturns_SalesReturnId] FOREIGN KEY ([SalesReturnId]) REFERENCES [SalesReturns] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE INDEX [IX_PurchaseReturnItems_ProductId] ON [PurchaseReturnItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE INDEX [IX_PurchaseReturnItems_PurchaseItemId] ON [PurchaseReturnItems] ([PurchaseItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE INDEX [IX_PurchaseReturnItems_PurchaseReturnId] ON [PurchaseReturnItems] ([PurchaseReturnId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE INDEX [IX_PurchaseReturns_PurchaseId] ON [PurchaseReturns] ([PurchaseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseReturns_ReturnNumber] ON [PurchaseReturns] ([ReturnNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE INDEX [IX_SalesReturnItems_ProductId] ON [SalesReturnItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE INDEX [IX_SalesReturnItems_SaleItemId] ON [SalesReturnItems] ([SaleItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE INDEX [IX_SalesReturnItems_SalesReturnId] ON [SalesReturnItems] ([SalesReturnId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesReturns_ReturnNumber] ON [SalesReturns] ([ReturnNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    CREATE INDEX [IX_SalesReturns_SaleId] ON [SalesReturns] ([SaleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080339_AddReturnDocuments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028080339_AddReturnDocuments', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080346_AddAuditLogAndStaffRole'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [OccurredOn] datetime2 NOT NULL,
        [UserId] int NULL,
        [UserEmail] nvarchar(200) NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(100) NULL,
        [EntityId] nvarchar(50) NULL,
        [Details] nvarchar(2000) NULL,
        [IpAddress] nvarchar(45) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080346_AddAuditLogAndStaffRole'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityType_EntityId] ON [AuditLogs] ([EntityType], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080346_AddAuditLogAndStaffRole'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_OccurredOn] ON [AuditLogs] ([OccurredOn]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080346_AddAuditLogAndStaffRole'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Staff')
        INSERT INTO Roles (Name, CreatedOn, IsDeleted) VALUES ('Staff', SYSUTCDATETIME(), 0);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028080346_AddAuditLogAndStaffRole'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028080346_AddAuditLogAndStaffRole', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    ALTER TABLE [Sales] ADD [DocumentSnapshot] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    ALTER TABLE [Payments] ADD [ReceiptNumber] nvarchar(16) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    ALTER TABLE [Companies] ADD [DefaultInvoiceFormat] nvarchar(20) NOT NULL DEFAULT N'A4';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    ALTER TABLE [Companies] ADD [ShowBankDetails] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    ALTER TABLE [Companies] ADD [ShowFooter] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    ALTER TABLE [Companies] ADD [ShowLogo] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    ALTER TABLE [Companies] ADD [ShowTerms] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    ALTER TABLE [Companies] ADD [ThermalPaperWidthMm] int NOT NULL DEFAULT 80;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Payments_ReceiptNumber] ON [Payments] ([ReceiptNumber]) WHERE [ReceiptNumber] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    EXEC(N'ALTER TABLE [Companies] ADD CONSTRAINT [CK_Companies_ThermalWidth] CHECK ([ThermalPaperWidthMm] IN (58, 80))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028085125_AddInvoiceSnapshotReceiptsAndPrintSettings', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    ALTER TABLE [Products] ADD [Barcode] nvarchar(80) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    ALTER TABLE [Products] ADD [RequiresSerialNumber] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    ALTER TABLE [Products] ADD [WarrantyDurationUnit] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    ALTER TABLE [Products] ADD [WarrantyDurationValue] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    ALTER TABLE [Companies] ADD [ShopType] nvarchar(30) NOT NULL DEFAULT N'GeneralRetail';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE TABLE [Warranties] (
        [Id] int NOT NULL IDENTITY,
        [WarrantyNumber] nvarchar(16) NOT NULL,
        [SaleId] int NOT NULL,
        [SaleItemId] int NOT NULL,
        [ProductId] int NOT NULL,
        [CustomerId] int NULL,
        [InvoiceNumber] nvarchar(16) NOT NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [ProductSku] nvarchar(200) NULL,
        [CustomerName] nvarchar(200) NOT NULL,
        [CustomerMobile] nvarchar(20) NOT NULL,
        [SerialNumber] nvarchar(80) NULL,
        [DurationValue] int NOT NULL,
        [DurationUnit] nvarchar(20) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [IsVoided] bit NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [modifieson] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Warranties] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warranties_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Warranties_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Warranties_SaleItems_SaleItemId] FOREIGN KEY ([SaleItemId]) REFERENCES [SaleItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Warranties_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE TABLE [WarrantyClaims] (
        [Id] int NOT NULL IDENTITY,
        [ClaimNumber] nvarchar(16) NOT NULL,
        [WarrantyId] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [ReportedProblem] nvarchar(1000) NOT NULL,
        [InspectionNotes] nvarchar(2000) NULL,
        [DecisionReason] nvarchar(1000) NULL,
        [Notes] nvarchar(1000) NULL,
        [DefectiveSerial] nvarchar(80) NULL,
        [ReplacementProductId] int NULL,
        [ReplacementProductName] nvarchar(200) NULL,
        [ReplacementSerial] nvarchar(80) NULL,
        [ReplacementStockMoved] bit NOT NULL,
        [SubmittedOn] datetime2 NOT NULL,
        [ResolvedOn] datetime2 NULL,
        [CreatedOn] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [modifieson] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_WarrantyClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarrantyClaims_Products_ReplacementProductId] FOREIGN KEY ([ReplacementProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarrantyClaims_Warranties_WarrantyId] FOREIGN KEY ([WarrantyId]) REFERENCES [Warranties] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE TABLE [WarrantyClaimEvents] (
        [Id] int NOT NULL IDENTITY,
        [WarrantyClaimId] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedOn] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [modifieson] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_WarrantyClaimEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarrantyClaimEvents_WarrantyClaims_WarrantyClaimId] FOREIGN KEY ([WarrantyClaimId]) REFERENCES [WarrantyClaims] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_Barcode] ON [Products] ([Barcode]) WHERE [Barcode] IS NOT NULL AND [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE INDEX [IX_Warranties_CustomerId] ON [Warranties] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE INDEX [IX_Warranties_CustomerMobile] ON [Warranties] ([CustomerMobile]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE INDEX [IX_Warranties_InvoiceNumber] ON [Warranties] ([InvoiceNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE INDEX [IX_Warranties_ProductId] ON [Warranties] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE INDEX [IX_Warranties_SaleId] ON [Warranties] ([SaleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE INDEX [IX_Warranties_SaleItemId] ON [Warranties] ([SaleItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Warranties_SerialNumber] ON [Warranties] ([SerialNumber]) WHERE [SerialNumber] IS NOT NULL AND [IsVoided] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Warranties_WarrantyNumber] ON [Warranties] ([WarrantyNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE INDEX [IX_WarrantyClaimEvents_WarrantyClaimId] ON [WarrantyClaimEvents] ([WarrantyClaimId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarrantyClaims_ClaimNumber] ON [WarrantyClaims] ([ClaimNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE INDEX [IX_WarrantyClaims_ReplacementProductId] ON [WarrantyClaims] ([ReplacementProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    CREATE INDEX [IX_WarrantyClaims_WarrantyId] ON [WarrantyClaims] ([WarrantyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028114940_AddRetailWarrantyAndBarcode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028114940_AddRetailWarrantyAndBarcode', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029063526_AddProductImportBatches'
)
BEGIN
    CREATE TABLE [ProductImportBatches] (
        [Id] int NOT NULL IDENTITY,
        [FileHash] nvarchar(64) NOT NULL,
        [FileName] nvarchar(260) NULL,
        [ResultJson] nvarchar(max) NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [modifieson] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_ProductImportBatches] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029063526_AddProductImportBatches'
)
BEGIN
    CREATE TABLE [ProductImportReceipts] (
        [Id] int NOT NULL IDENTITY,
        [BatchId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ReceiptKey] nvarchar(200) NOT NULL,
        [Quantity] int NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [modifieson] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_ProductImportReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductImportReceipts_ProductImportBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductImportBatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductImportReceipts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029063526_AddProductImportBatches'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductImportBatches_FileHash] ON [ProductImportBatches] ([FileHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029063526_AddProductImportBatches'
)
BEGIN
    CREATE INDEX [IX_ProductImportReceipts_BatchId] ON [ProductImportReceipts] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029063526_AddProductImportBatches'
)
BEGIN
    CREATE INDEX [IX_ProductImportReceipts_ProductId] ON [ProductImportReceipts] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029063526_AddProductImportBatches'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductImportReceipts_ReceiptKey] ON [ProductImportReceipts] ([ReceiptKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029063526_AddProductImportBatches'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029063526_AddProductImportBatches', N'8.0.18');
END;
GO

COMMIT;
GO


