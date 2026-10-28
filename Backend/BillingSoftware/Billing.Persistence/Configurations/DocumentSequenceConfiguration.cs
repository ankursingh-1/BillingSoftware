using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Persistence.Configurations;

public class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable("DocumentSequences");

        builder.HasKey(x => x.SeriesKey);

        builder.Property(x => x.SeriesKey)
               .HasMaxLength(50)
               .ValueGeneratedNever();
    }
}
