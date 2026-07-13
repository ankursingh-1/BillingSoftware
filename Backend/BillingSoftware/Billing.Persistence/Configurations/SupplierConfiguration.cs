using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(x => x.CompanyName)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(x => x.Mobile)
               .HasMaxLength(15)
               .IsRequired();

        builder.Property(x => x.Email)
               .HasMaxLength(200);

        builder.Property(x => x.Address)
               .HasMaxLength(500);
    }
}