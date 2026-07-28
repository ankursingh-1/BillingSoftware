using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.Property(x => x.Title)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.Amount)
                   .HasPrecision(18, 2);

            builder.Property(x => x.Category)
                   .HasMaxLength(100);

            builder.Property(x => x.Remarks)
                   .HasMaxLength(500);
        }
    }
}