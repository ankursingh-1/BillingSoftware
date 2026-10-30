using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class WarrantyConfiguration : IEntityTypeConfiguration<Warranty>
{
    public void Configure(EntityTypeBuilder<Warranty> builder)
    {
        builder.ToTable("Warranties");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WarrantyNumber)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(x => x.WarrantyNumber).IsUnique();

        builder.Property(x => x.InvoiceNumber).HasMaxLength(16).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProductSku).HasMaxLength(200);
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CustomerMobile).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(80);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.Property(x => x.DurationUnit)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(x => x.InvoiceNumber);
        builder.HasIndex(x => x.CustomerMobile);

        builder.HasIndex(x => x.SerialNumber)
            .IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL AND [IsVoided] = 0");

        builder.HasOne(x => x.Sale)
            .WithMany(s => s.Warranties)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SaleItem)
            .WithMany(i => i.Warranties)
            .HasForeignKey(x => x.SaleItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
