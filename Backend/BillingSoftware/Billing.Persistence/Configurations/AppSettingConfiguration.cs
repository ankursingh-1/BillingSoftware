using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations
{
    public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
    {
        public void Configure(EntityTypeBuilder<AppSetting> builder)
        {
            builder.Property(x => x.CompanyName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.CompanyAddress)
                .HasMaxLength(500);

            builder.Property(x => x.Phone)
                .HasMaxLength(20);

            builder.Property(x => x.Email)
                .HasMaxLength(100);

            builder.Property(x => x.GSTNumber)
                .HasMaxLength(50);

            builder.Property(x => x.Currency)
                .HasMaxLength(20);

            builder.Property(x => x.InvoicePrefix)
                .HasMaxLength(20);

            builder.Property(x => x.LogoUrl)
                .HasMaxLength(500);
        }
    }
}