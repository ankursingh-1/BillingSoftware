using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
            table.HasCheckConstraint("CK_Payments_Amount_Positive", "[Amount] > 0"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Method).HasConversion<int>();
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.VoidedBy).HasMaxLength(200);
        builder.Property(x => x.VoidReason).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(200);

        builder.HasIndex(x => x.SaleId);

        builder.HasOne(x => x.Sale)
               .WithMany(x => x.Payments)
               .HasForeignKey(x => x.SaleId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
