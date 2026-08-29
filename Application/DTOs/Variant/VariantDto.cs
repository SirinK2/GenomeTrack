using System;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Application.DTOs.Variant;

public class VariantCallDto
{
    public Guid Id { get; set; }
    public Guid SampleId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Gene { get; set; } = string.Empty;
    public string Chromosome { get; set; } = string.Empty;
    public long Position { get; set; }
    public string ReferenceAllele { get; set; } = string.Empty;
    public string AlternateAllele { get; set; } = string.Empty;
    public ClinicalSignificance Significance { get; set; }
    public int ReadDepth { get; set; }
    public decimal QualityScore { get; set; }
    public Zygosity Zygosity { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public bool IsReleased { get; set; }
}

public class VariantCallFilter
{
    public Guid? SampleId { get; set; }
    public string? Gene { get; set; }
    public ClinicalSignificance? MinimumSignificance { get; set; }
    public bool? ReleasedOnly { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
