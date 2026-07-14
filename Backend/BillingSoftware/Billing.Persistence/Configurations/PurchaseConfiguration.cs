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

        builder.Property(x => x.TotalAmount)
               .HasPrecision(18, 2);

        builder.Property(x => x.Remarks)
               .HasMaxLength(500);

        builder.HasOne(x => x.Supplier)
               .WithMany()
               .HasForeignKey(x => x.SupplierId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}