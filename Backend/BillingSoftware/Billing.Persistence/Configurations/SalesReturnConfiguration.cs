using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("SalesReturns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReturnNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ReturnNumber).IsUnique();
        builder.HasIndex(x => x.SaleId);

        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(200);

        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.CgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.SgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.IgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.RefundAmount).HasPrecision(18, 2);
        builder.Property(x => x.RefundMethod).HasConversion<int?>();

        builder.HasOne(x => x.Sale)
               .WithMany()
               .HasForeignKey(x => x.SaleId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalesReturnItemConfiguration : IEntityTypeConfiguration<SalesReturnItem>
{
    public void Configure(EntityTypeBuilder<SalesReturnItem> builder)
    {
        builder.ToTable("SalesReturnItems", table =>
            table.HasCheckConstraint("CK_SalesReturnItems_Quantity_Positive", "[Quantity] > 0"));

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.SaleItemId);

        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);
        builder.Property(x => x.CgstAmount).HasPrecision(18, 2);
        builder.Property(x => x.SgstAmount).HasPrecision(18, 2);
        builder.Property(x => x.IgstAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);

        builder.HasOne(x => x.SalesReturn)
               .WithMany(x => x.Items)
               .HasForeignKey(x => x.SalesReturnId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SaleItem)
               .WithMany()
               .HasForeignKey(x => x.SaleItemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
