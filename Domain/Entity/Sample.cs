using System;
using System.Collections.Generic;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Domain.Entity;

/// <summary>
/// A physical specimen. <see cref="Barcode"/> is the lab's identifier and is unique across
/// live samples; <see cref="SubjectRef"/> is the pseudonymised subject key. The two are kept
/// apart on purpose — the barcode travels on the tube where anyone in the building can read
/// it, so it must not carry anything that identifies a person.
/// </summary>
public class Sample : BaseEntity
{
    public string Barcode { get; set; } = string.Empty;
    public string SubjectRef { get; set; } = string.Empty;
    public SampleType Type { get; set; }
    public SampleStatus Status { get; set; } = SampleStatus.Registered;
    public DateTimeOffset CollectedAt { get; set; }
    public string CurrentLocation { get; set; } = string.Empty;

    public ICollection<CustodyEvent> CustodyEvents { get; set; } = new List<CustodyEvent>();
    public ICollection<RunSample> RunSamples { get; set; } = new List<RunSample>();
    public ICollection<VariantCall> VariantCalls { get; set; } = new List<VariantCall>();
}
