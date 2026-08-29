using System;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Application.DTOs.Sample;

public class SampleDto
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string SubjectRef { get; set; } = string.Empty;
    public SampleType Type { get; set; }
    public SampleStatus Status { get; set; }
    public DateTimeOffset CollectedAt { get; set; }
    public string CurrentLocation { get; set; } = string.Empty;
}

public class RegisterSampleDto
{
    public string Barcode { get; set; } = string.Empty;
    public string SubjectRef { get; set; } = string.Empty;
    public SampleType Type { get; set; }
    public DateTimeOffset CollectedAt { get; set; }
    public string CollectedAtLocation { get; set; } = string.Empty;
}

public class AccessionSampleDto
{
    public string ReceivedAtLocation { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class SampleFilter
{
    public string? Barcode { get; set; }
    public string? SubjectRef { get; set; }
    public SampleStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
