-- REVIEW ONLY. Do not execute without explicit approval.
-- Generated 2026-10-29: dotnet ef migrations script 20261028114940_AddRetailWarrantyAndBarcode --idempotent
-- Adds ProductImportBatches / ProductImportReceipts. Not applied.
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


