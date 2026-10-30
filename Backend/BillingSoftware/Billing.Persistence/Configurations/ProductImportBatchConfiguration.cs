using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class ProductImportBatchConfiguration : IEntityTypeConfiguration<ProductImportBatch>
{
    public void Configure(EntityTypeBuilder<ProductImportBatch> builder)
    {
        builder.ToTable("ProductImportBatches");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.FileHash)
            .IsUnique();

        builder.Property(x => x.FileName)
            .HasMaxLength(260);

        builder.Property(x => x.ResultJson)
            .IsRequired();
    }
}

public class ProductImportReceiptConfiguration : IEntityTypeConfiguration<ProductImportReceipt>
{
    public void Configure(EntityTypeBuilder<ProductImportReceipt> builder)
    {
        builder.ToTable("ProductImportReceipts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReceiptKey)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.ReceiptKey)
            .IsUnique();

        builder.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
