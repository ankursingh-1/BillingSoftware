-- NOT EXECUTED. Run only after the backup in VERIFY-ONLY-AddProductImportBatches.md.
-- One batch on purpose: a failed guard must not continue into later GO batches.
-- Refuses every database except BillingSoftware_Verify. Does not touch shop rows.
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF DB_NAME() <> N'BillingSoftware_Verify'
        THROW 51000, 'Refusing: connected database is not BillingSoftware_Verify. Do not run this on BillingSoftwareDb.', 1;

    IF (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) <> 21
        THROW 51001, 'Refusing: BillingSoftware_Verify must have exactly 21 applied migrations before AddProductImportBatches.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.__EFMigrationsHistory
        WHERE MigrationId = N'20261029063526_AddProductImportBatches')
        THROW 51002, 'Refusing: AddProductImportBatches is already in history.', 1;

    IF OBJECT_ID(N'dbo.ProductImportBatches', N'U') IS NOT NULL
        OR OBJECT_ID(N'dbo.ProductImportReceipts', N'U') IS NOT NULL
        THROW 51003, 'Refusing: import tables already exist.', 1;

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
        CONSTRAINT [FK_ProductImportReceipts_ProductImportBatches_BatchId]
            FOREIGN KEY ([BatchId]) REFERENCES [ProductImportBatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductImportReceipts_Products_ProductId]
            FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_ProductImportBatches_FileHash] ON [ProductImportBatches] ([FileHash]);
    CREATE INDEX [IX_ProductImportReceipts_BatchId] ON [ProductImportReceipts] ([BatchId]);
    CREATE INDEX [IX_ProductImportReceipts_ProductId] ON [ProductImportReceipts] ([ProductId]);
    CREATE UNIQUE INDEX [IX_ProductImportReceipts_ReceiptKey] ON [ProductImportReceipts] ([ReceiptKey]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029063526_AddProductImportBatches', N'8.0.18');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
