using GenomeTrack.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenomeTrack.Infrastructure.Mappings;

public class SampleConfiguration : IEntityTypeConfiguration<Sample>
{
    public void Configure(EntityTypeBuilder<Sample> builder)
    {
        builder.ToTable("samples");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Barcode).HasMaxLength(64).IsRequired();
        builder.Property(s => s.SubjectRef).HasMaxLength(64).IsRequired();
        builder.Property(s => s.CurrentLocation).HasMaxLength(128);

        // Filtered so a barcode can be reused once its sample is gone, while two live samples
        // can never share one.
        builder
            .HasIndex(s => s.Barcode)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(s => s.SubjectRef);
        builder.HasIndex(s => s.Status);

    }
}
