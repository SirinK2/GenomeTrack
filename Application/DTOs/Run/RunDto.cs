using System;
using System.Collections.Generic;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Application.DTOs.Run;

public class SequencingRunDto
{
    public Guid Id { get; set; }
    public string RunCode { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public RunStatus Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public List<RunSampleDto> Samples { get; set; } = new();
}

public class RunSampleDto
{
    public Guid SampleId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int LaneIndex { get; set; }
}

public class CreateRunDto
{
    public string RunCode { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
}

public class LoadSampleDto
{
    public Guid SampleId { get; set; }
    public int LaneIndex { get; set; }
}

public class CompleteRunDto
{
    public List<VariantCallInputDto> Calls { get; set; } = new();
}

public class VariantCallInputDto
{
    public Guid SampleId { get; set; }
    public string Gene { get; set; } = string.Empty;
    public string Chromosome { get; set; } = string.Empty;
    public long Position { get; set; }
    public string ReferenceAllele { get; set; } = string.Empty;
    public string AlternateAllele { get; set; } = string.Empty;
    public ClinicalSignificance Significance { get; set; }
    public int ReadDepth { get; set; }
    public decimal QualityScore { get; set; }
    public Zygosity Zygosity { get; set; }
}
