using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PurchaseNumber)
               .HasMaxLength(50)
               .IsRequired();

        builder.HasIndex(x => x.PurchaseNumber)
               .IsUnique();

        builder.HasIndex(x => x.ClientRequestId)
               .IsUnique()
               .HasFilter("[ClientRequestId] IS NOT NULL");

        builder.HasIndex(x => x.PurchaseDate);
        builder.HasIndex(x => x.SupplierId);

        builder.Property(x => x.SupplierInvoiceNumber).HasMaxLength(50);
        builder.Property(x => x.SupplierGstin).HasMaxLength(15);

        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.CgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.SgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.IgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
        builder.Property(x => x.RoundOff).HasPrecision(18, 2);

        builder.Property(x => x.Remarks)
               .HasMaxLength(500);

        builder.HasOne(x => x.Supplier)
               .WithMany()
               .HasForeignKey(x => x.SupplierId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
