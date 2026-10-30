using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Stock can never be negative, even if application code has a bug.
        builder.ToTable("Products", table =>
            table.HasCheckConstraint(
                "CK_Products_Stock_NonNegative",
                "[Stock] >= 0"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(x => x.SKU)
               .HasMaxLength(200);

        builder.Property(x => x.Barcode)
               .HasMaxLength(80);

        builder.HasIndex(x => x.Barcode)
               .IsUnique()
               .HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");

        builder.Property(x => x.HsnCode)
               .HasMaxLength(8);

        builder.Property(x => x.WarrantyDurationUnit)
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(x => x.PurchasePrice)
               .HasPrecision(18, 2);

        builder.Property(x => x.SellingPrice)
               .HasPrecision(18, 2);
    }
}
