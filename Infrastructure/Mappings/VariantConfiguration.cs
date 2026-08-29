using GenomeTrack.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenomeTrack.Infrastructure.Mappings;

public class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        builder.ToTable("variants");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Gene).HasMaxLength(64).IsRequired();
        builder.Property(v => v.Chromosome).HasMaxLength(8).IsRequired();
        builder.Property(v => v.ReferenceAllele).HasMaxLength(256).IsRequired();
        builder.Property(v => v.AlternateAllele).HasMaxLength(256).IsRequired();
        builder.Property(v => v.ClinVarId).HasMaxLength(32);

        // Identity is the locus plus both alleles — same position, different substitution is a
        // different variant.
        builder
            .HasIndex(v => new
            {
                v.Chromosome,
                v.Position,
                v.ReferenceAllele,
                v.AlternateAllele,
            })
            .IsUnique();

        builder.HasIndex(v => v.Gene);
    }
}

public class VariantCallConfiguration : IEntityTypeConfiguration<VariantCall>
{
    public void Configure(EntityTypeBuilder<VariantCall> builder)
    {
        builder.ToTable("variant_calls");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.QualityScore).HasPrecision(6, 2);
        builder.Ignore(c => c.IsReleased);

        // One call per variant per sample per run. A re-run produces a new row because the run
        // differs, which is what keeps re-analysis history intact.
        builder
            .HasIndex(c => new
            {
                c.SampleId,
                c.VariantId,
                c.SequencingRunId,
            })
            .IsUnique();

        builder
            .HasOne(c => c.Sample)
            .WithMany(s => s.VariantCalls)
            .HasForeignKey(c => c.SampleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(c => c.Variant)
            .WithMany(v => v.Calls)
            .HasForeignKey(c => c.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(c => c.SequencingRun)
            .WithMany(r => r.VariantCalls)
            .HasForeignKey(c => c.SequencingRunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(c => c.ReleasedBy)
            .WithMany()
            .HasForeignKey(c => c.ReleasedById)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
