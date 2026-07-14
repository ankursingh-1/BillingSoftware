using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PurchasePrice)
               .HasPrecision(18, 2);

        builder.Property(x => x.Total)
               .HasPrecision(18, 2);

        builder.HasOne(x => x.Purchase)
               .WithMany(x => x.PurchaseItems)
               .HasForeignKey(x => x.PurchaseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}