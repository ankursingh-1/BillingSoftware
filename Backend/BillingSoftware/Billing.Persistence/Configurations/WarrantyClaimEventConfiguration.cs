using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class WarrantyClaimEventConfiguration : IEntityTypeConfiguration<WarrantyClaimEvent>
{
    public void Configure(EntityTypeBuilder<WarrantyClaimEvent> builder)
    {
        builder.ToTable("WarrantyClaimEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.WarrantyClaim)
            .WithMany(c => c.Events)
            .HasForeignKey(x => x.WarrantyClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
