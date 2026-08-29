using System;
using System.Collections.Generic;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Domain.Entity;

public class SequencingRun : BaseEntity
{
    public string RunCode { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public RunStatus Status { get; set; } = RunStatus.Draft;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Guid CreatedById { get; set; }
    public LabUser? CreatedBy { get; set; }

    public string? FailureReason { get; set; }

    public ICollection<RunSample> RunSamples { get; set; } = new List<RunSample>();
    public ICollection<VariantCall> VariantCalls { get; set; } = new List<VariantCall>();
}
