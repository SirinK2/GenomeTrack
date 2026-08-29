using GenomeTrack.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenomeTrack.Infrastructure.Mappings;

public class CustodyEventConfiguration : IEntityTypeConfiguration<CustodyEvent>
{
    public void Configure(EntityTypeBuilder<CustodyEvent> builder)
    {
        builder.ToTable("custody_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FromLocation).HasMaxLength(128);
        builder.Property(e => e.ToLocation).HasMaxLength(128);
        builder.Property(e => e.PreviousHash).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Hash).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Note).HasMaxLength(512);

        // Two events cannot claim the same position in one sample's chain. Without this the
        // verifier's sequence check could be satisfied by a duplicate rather than the original.
        builder.HasIndex(e => new { e.SampleId, e.Sequence }).IsUnique();

        builder
            .HasOne(e => e.Sample)
            .WithMany(s => s.CustodyEvents)
            .HasForeignKey(e => e.SampleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
