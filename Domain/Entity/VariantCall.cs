using System;

namespace GenomeTrack.Domain.Entity;

/// <summary>
/// An observation of a <see cref="Variant"/> in one sample on one run.
///
/// A call is provisional until a principal investigator releases it. Before release it is
/// visible to the lab and to nobody else; after release <see cref="ReleasedAt"/> is stamped
/// and the row is frozen. Re-running a sample produces a new call rather than editing the old
/// one, so the record of what was reported, and when, survives a re-analysis.
/// </summary>
public class VariantCall : BaseEntity
{
    public Guid SampleId { get; set; }
    public Sample? Sample { get; set; }

    public Guid VariantId { get; set; }
    public Variant? Variant { get; set; }

    public Guid SequencingRunId { get; set; }
    public SequencingRun? SequencingRun { get; set; }

    public int ReadDepth { get; set; }
    public decimal QualityScore { get; set; }
    public GenomeTrack.Domain.Enums.Zygosity Zygosity { get; set; }

    public DateTimeOffset? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
    public LabUser? ReleasedBy { get; set; }

    public bool IsReleased => ReleasedAt.HasValue;
}
