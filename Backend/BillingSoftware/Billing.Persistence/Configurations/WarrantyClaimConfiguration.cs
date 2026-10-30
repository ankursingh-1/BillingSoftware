using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class WarrantyClaimConfiguration : IEntityTypeConfiguration<WarrantyClaim>
{
    public void Configure(EntityTypeBuilder<WarrantyClaim> builder)
    {
        builder.ToTable("WarrantyClaims");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClaimNumber)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(x => x.ClaimNumber).IsUnique();

        builder.Property(x => x.ReportedProblem).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.InspectionNotes).HasMaxLength(2000);
        builder.Property(x => x.DecisionReason).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.DefectiveSerial).HasMaxLength(80);
        builder.Property(x => x.ReplacementProductName).HasMaxLength(200);
        builder.Property(x => x.ReplacementSerial).HasMaxLength(80);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasOne(x => x.Warranty)
            .WithMany(w => w.Claims)
            .HasForeignKey(x => x.WarrantyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReplacementProduct)
            .WithMany()
            .HasForeignKey(x => x.ReplacementProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
