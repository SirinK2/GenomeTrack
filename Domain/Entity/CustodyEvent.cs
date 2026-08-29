using System;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Domain.Entity;

/// <summary>
/// One append-only link in a sample's chain of custody.
///
/// Each event stores the hash of the event before it and a hash of its own contents, so the
/// chain is tamper-evident: editing or deleting any historical row breaks every hash after
/// it, and <c>VerifyChainAsync</c> will say exactly where. This is the difference between an
/// audit log and an audit log you can trust — a plain table can be quietly UPDATEd by anyone
/// with database access, and a custody record that can be rewritten is worth nothing in a
/// dispute over whose sample produced a result.
///
/// Nothing here is ever mutated after insert. There is no setter path in the service that
/// updates an existing event, and the EF configuration marks the row immutable.
/// </summary>
public class CustodyEvent : BaseEntity
{
    public Guid SampleId { get; set; }
    public Sample? Sample { get; set; }

    public CustodyAction Action { get; set; }
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;

    public Guid ActorId { get; set; }
    public LabUser? Actor { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>Position in the chain, starting at 1. Unique per sample.</summary>
    public int Sequence { get; set; }

    /// <summary>Hash of the preceding event, or the genesis constant for the first link.</summary>
    public string PreviousHash { get; set; } = string.Empty;

    /// <summary>Hash over this event's own contents plus <see cref="PreviousHash"/>.</summary>
    public string Hash { get; set; } = string.Empty;

    public string? Note { get; set; }
}
