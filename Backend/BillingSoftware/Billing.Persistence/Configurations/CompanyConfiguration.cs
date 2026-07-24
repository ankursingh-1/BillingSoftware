using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CompanyName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.GSTNumber)
                   .HasMaxLength(30);

            builder.Property(x => x.PANNumber)
                   .HasMaxLength(20);

            builder.Property(x => x.Address)
                   .HasMaxLength(500);

            builder.Property(x => x.City)
                   .HasMaxLength(100);

            builder.Property(x => x.State)
                   .HasMaxLength(100);

            builder.Property(x => x.Country)
                   .HasMaxLength(100);

            builder.Property(x => x.Pincode)
                   .HasMaxLength(20);

            builder.Property(x => x.Phone)
                   .HasMaxLength(20);

            builder.Property(x => x.Email)
                   .HasMaxLength(150);

            builder.Property(x => x.Website)
                   .HasMaxLength(200);

            builder.Property(x => x.LogoPath)
                   .HasMaxLength(500);

            builder.Property(x => x.InvoicePrefix)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.Currency)
                   .IsRequired()
                   .HasMaxLength(10);

            builder.Property(x => x.BankName)
                   .HasMaxLength(150);

            builder.Property(x => x.AccountNumber)
                   .HasMaxLength(50);

            builder.Property(x => x.IFSCCode)
                   .HasMaxLength(20);

            builder.Property(x => x.UPIId)
                   .HasMaxLength(100);

            builder.Property(x => x.TermsAndConditions)
                   .HasMaxLength(2000);

            builder.Property(x => x.FooterMessage)
                   .HasMaxLength(1000);
        }
    }
}