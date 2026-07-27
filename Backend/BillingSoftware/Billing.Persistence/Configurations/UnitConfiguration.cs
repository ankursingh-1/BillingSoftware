using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .HasMaxLength(100)
               .IsRequired();

        builder.HasIndex(x => x.Name)
               .IsUnique();

        builder.Property(x => x.ShortName)
               .HasMaxLength(20);

        builder.Property(x => x.Description)
               .HasMaxLength(500);

        builder.HasMany(x => x.Products)
               .WithOne(x => x.Unit)
               .HasForeignKey(x => x.UnitId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}