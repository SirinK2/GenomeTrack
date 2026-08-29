using System;

namespace GenomeTrack.Domain.Entity;

/// <summary>
/// A sample's place on a flow cell. <see cref="LaneIndex"/> and the sample are unique per run
/// so two libraries cannot be recorded in the same lane.
/// </summary>
public class RunSample : BaseEntity
{
    public Guid SequencingRunId { get; set; }
    public SequencingRun? SequencingRun { get; set; }

    public Guid SampleId { get; set; }
    public Sample? Sample { get; set; }

    public int LaneIndex { get; set; }
}
