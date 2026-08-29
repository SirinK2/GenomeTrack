using GenomeTrack.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenomeTrack.Infrastructure.Mappings;

public class SequencingRunConfiguration : IEntityTypeConfiguration<SequencingRun>
{
    public void Configure(EntityTypeBuilder<SequencingRun> builder)
    {
        builder.ToTable("sequencing_runs");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RunCode).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Platform).HasMaxLength(128);
        builder.Property(r => r.FailureReason).HasMaxLength(512);

        builder.HasIndex(r => r.RunCode).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder
            .HasOne(r => r.CreatedBy)
            .WithMany()
            .HasForeignKey(r => r.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

public class RunSampleConfiguration : IEntityTypeConfiguration<RunSample>
{
    public void Configure(EntityTypeBuilder<RunSample> builder)
    {
        builder.ToTable("run_samples");
        builder.HasKey(rs => rs.Id);

        builder.HasIndex(rs => new { rs.SequencingRunId, rs.SampleId }).IsUnique();
        builder.HasIndex(rs => new { rs.SequencingRunId, rs.LaneIndex }).IsUnique();

        builder
            .HasOne(rs => rs.SequencingRun)
            .WithMany(r => r.RunSamples)
            .HasForeignKey(rs => rs.SequencingRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(rs => rs.Sample)
            .WithMany(s => s.RunSamples)
            .HasForeignKey(rs => rs.SampleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
