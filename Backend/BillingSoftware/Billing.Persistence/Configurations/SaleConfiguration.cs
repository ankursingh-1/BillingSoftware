using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNumber)
               .HasMaxLength(50)
               .IsRequired();

        // The database, not application code, guarantees invoice numbers are unique.
        builder.HasIndex(x => x.InvoiceNumber)
               .IsUnique();

        // Makes a retried / double-clicked submit idempotent.
        builder.HasIndex(x => x.ClientRequestId)
               .IsUnique()
               .HasFilter("[ClientRequestId] IS NOT NULL");

        builder.HasIndex(x => x.SaleDate);
        builder.HasIndex(x => x.CustomerId);

        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.CgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.SgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.IgstTotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
        builder.Property(x => x.RoundOff).HasPrecision(18, 2);

        builder.Property(x => x.PlaceOfSupplyStateCode).HasMaxLength(2);
        builder.Property(x => x.CompanyStateCode).HasMaxLength(2);
        builder.Property(x => x.CustomerGstin).HasMaxLength(15);
        builder.Property(x => x.CompanyGstin).HasMaxLength(15);
        builder.Property(x => x.DocumentSnapshot).HasColumnType("nvarchar(max)");

        builder.Property(x => x.Remarks)
               .HasMaxLength(500);

        builder.HasOne(x => x.Customer)
               .WithMany()
               .HasForeignKey(x => x.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
