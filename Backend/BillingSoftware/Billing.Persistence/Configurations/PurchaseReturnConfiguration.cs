using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("PurchaseReturns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReturnNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ReturnNumber).IsUnique();
        builder.HasIndex(x => x.PurchaseId);

        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.AdjustmentNote).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(200);

        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.CgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.SgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.IgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.RefundReceived).HasPrecision(18, 2);
        builder.Property(x => x.RefundMethod).HasConversion<int?>();

        builder.HasOne(x => x.Purchase)
               .WithMany()
               .HasForeignKey(x => x.PurchaseId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseReturnItemConfiguration : IEntityTypeConfiguration<PurchaseReturnItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnItem> builder)
    {
        builder.ToTable("PurchaseReturnItems", table =>
            table.HasCheckConstraint("CK_PurchaseReturnItems_Quantity_Positive", "[Quantity] > 0"));

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PurchaseItemId);

        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);
        builder.Property(x => x.CgstAmount).HasPrecision(18, 2);
        builder.Property(x => x.SgstAmount).HasPrecision(18, 2);
        builder.Property(x => x.IgstAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);

        builder.HasOne(x => x.PurchaseReturn)
               .WithMany(x => x.Items)
               .HasForeignKey(x => x.PurchaseReturnId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PurchaseItem)
               .WithMany()
               .HasForeignKey(x => x.PurchaseItemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
