using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations
{
    public class StockLedgerConfiguration : IEntityTypeConfiguration<StockLedger>
    {
        public void Configure(EntityTypeBuilder<StockLedger> builder)
        {
            builder.ToTable("StockLedgers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                   .IsRequired();

            builder.Property(x => x.PreviousStock)
                   .IsRequired();

            builder.Property(x => x.CurrentStock)
                   .IsRequired();

            builder.Property(x => x.ReferenceNo)
                   .HasMaxLength(100);

            builder.Property(x => x.Remarks)
                   .HasMaxLength(500);

            builder.HasOne(x => x.Product)
                   .WithMany(x => x.StockLedgers)
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}