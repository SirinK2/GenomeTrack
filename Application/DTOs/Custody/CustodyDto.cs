using System;
using System.Collections.Generic;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Application.DTOs.Custody;

public class CustodyEventDto
{
    public Guid Id { get; set; }
    public int Sequence { get; set; }
    public CustodyAction Action { get; set; }
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class TransferCustodyDto
{
    public CustodyAction Action { get; set; }
    public string ToLocation { get; set; } = string.Empty;
    public string? Note { get; set; }
}

/// <summary>
/// The outcome of re-hashing a sample's chain. <see cref="BrokenAtSequence"/> is the first link
/// whose stored hash disagrees with its recomputed one — everything before it is intact, so it
/// points at the row that was altered rather than merely reporting that something is wrong.
/// </summary>
public class ChainVerificationDto
{
    public Guid SampleId { get; set; }
    public bool IsIntact { get; set; }
    public int EventCount { get; set; }
    public int? BrokenAtSequence { get; set; }
    public string? Explanation { get; set; }
    public List<CustodyEventDto> Events { get; set; } = new();
}
